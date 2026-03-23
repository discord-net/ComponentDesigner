using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes;

public sealed record FunctionalState : ComponentState
{
    public ICSharpMethodSymbol Symbol { get; init; }
    public override IReadOnlyList<ComponentProperty> Properties { get; }
    public ComponentProperty? ChildrenParameter { get; init; }

    public int SymbolDependencyKey => _dependencyKey ??= MakeSymbolDependencyKey();

    public new CXElement CXNode { get; init; }

    private int? _dependencyKey;

    public FunctionalState(
        CXElement element,
        ICSharpMethodSymbol symbol,
        IReadOnlyList<ComponentProperty> properties,
        ComponentProperty? childrenParameter,
        ComponentNodeInitializationContext context,
        CancellationToken cancellationToken
    ) : base(context, cancellationToken, initialize: false)
    {
        CXNode = element;
        Symbol = symbol;
        Properties = properties;
        ChildrenParameter = childrenParameter;
        
        Initialize(context, cancellationToken);
    }

    public static Result<FunctionalState> CreateFromSymbol(
        ComponentNodeInitializationContext initializationContext,
        ICSharpMethodSymbol symbol,
        GraphNode graphNode,
        CXElement element,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    )
    {
        if (initializationContext.ComponentTypingProvider is null)
            return Diagnostic
                .TypedComponentsAreNotSupported(initializationContext.GraphContext.Implementation)
                .At(element);

        if (
            !initializationContext.ComponentTypingProvider.IsValidComponentType(
                initializationContext.GraphContext,
                symbol.ReturnType,
                cancellationToken
            )
        )
        {
            return element.IdentifierTextSpanOrElementTextSpan.Report(
                Diagnostic.FunctionalComponentDoesntReturnAComponentType(symbol)
            );
        }

        using var _ = ObjectPool<List<ComponentProperty>>.GetScoped(out var properties);
        properties.Clear();

        var symbolDependencyKey = symbol.ReturnType.GetHashCode();
        ComponentProperty? childrenParameter = null;
        ICSharpParameterSymbol? childrenParameterSymbol = null;

        for (var i = 0; i < symbol.Parameters.Count; i++)
        {
            var parameter = symbol.Parameters[i];

            symbolDependencyKey = Hash.Combine(symbolDependencyKey, parameter.Type.ToQualifiedName());

            var parameterProperty = new ComponentProperty(
                parameter.Name,
                isOptional: parameter.HasDefaultValue,
                requiresValue: !parameter.Type.Equals(initializationContext.CompilationProvider.Boolean!),
                kind: ComponentPropertyValueKind.Any
            );

            if (IsChildParameter(parameter))
            {
                if (childrenParameterSymbol is not null)
                {
                    // duplicate children parameter
                    return element.Report(
                        Diagnostic.FunctionalComponentHasDuplicateChildParameter(
                            symbol,
                            childrenParameterSymbol,
                            parameter
                        )
                    );
                }

                childrenParameter = parameterProperty;
                childrenParameterSymbol = parameter;
            }

            properties.Add(parameterProperty);
        }

        var state = new FunctionalState(
            element,
            symbol,
            [..properties],
            childrenParameter,
            initializationContext,
            cancellationToken
        );

        if (childrenParameter is not null && childrenParameterSymbol is not null)
        {
            if (
                initializationContext.ComponentTypingProvider.IsValidComponentType(
                    initializationContext.GraphContext,
                    childrenParameterSymbol.Type,
                    cancellationToken
                )
            )
            {
                initializationContext.PushAsChildren(element.Children, cancellationToken);

                state.SetPropertyValueToChildren(childrenParameter);
            }
            else
            {
                using var __ = List<ComponentPropertyValue>.Pooled(out var values);
                values.Clear();

                foreach (var child in element.Children)
                {
                    if (child is not CXValue cxValue)
                    {
                        diagnostics.Add(
                            Diagnostic
                                .ComponentDoesntAllowChildren(symbol.Name)
                                .At(child)
                        );

                        continue;
                    }

                    values.Add(
                        state.BuildPropertyValueFromSyntax(
                            initializationContext,
                            childrenParameter,
                            state.ChildSource,
                            cxValue,
                            cxValue.TextSpan,
                            cancellationToken
                        )
                    );
                }

                if (values.Count > 0)
                {
                    state.SetPropertyValue(
                        childrenParameter,
                        new ComponentPropertyValue.Many(
                            state.ChildSource,
                            childrenParameter,
                            [..values]
                        )
                    );
                }
            }
        }
        else
        {
            // push children anyway, let the validator handle diagnostics
            initializationContext.PushAsChildren(element.Children, cancellationToken);
        }

        return state;
    }

    public override bool TryGetProperty(string name, [MaybeNullWhen(false)] out ComponentProperty property)
    {
        property = Properties
            .FirstOrDefault(x => x.MatchesName(name));

        return property is not null || base.TryGetProperty(name, out property);
    }

    private static bool IsChildParameter(ICSharpParameterSymbol symbol)
        => symbol
            .GetAttributes()
            .Any(x => x
                .Type?.Name is "CXChildrenAttribute"
            );

    private int MakeSymbolDependencyKey()
    {
        var hash = Hash.Combine(
            Symbol.Name,
            Symbol.ContainingType.ToQualifiedName(),
            Symbol.ReturnType.ToQualifiedName()
        );

        for (var i = 0; i < Symbol.Parameters.Count; i++)
        {
            var parameter = Symbol.Parameters[i];

            hash = Hash.Combine(
                hash,
                parameter.Name,
                IsChildParameter(parameter),
                parameter.Type.Name
            );
        }

        return hash;
    }

    public bool Equals(FunctionalState? other)
        => other is not null &&
           SymbolDependencyKey == other.SymbolDependencyKey &&
           Properties.Equals(other.Properties) &&
           ChildrenParameter == other.ChildrenParameter &&
           base.Equals(other);

    public override int GetHashCode()
        => Hash.Combine(SymbolDependencyKey, Properties, ChildrenParameter, base.GetHashCode());
}