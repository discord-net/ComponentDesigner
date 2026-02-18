using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes;

public sealed record FunctionalState(
    GraphNode GraphNode,
    CXElement CXNode,
    ICSharpMethodSymbol Symbol,
    EquatableArray<ComponentProperty> Parameters,
    ComponentProperty? ChildrenParameter
) : ComponentState(GraphNode, CXNode)
{
    public int SymbolDependencyKey => _dependencyKey ??= MakeSymbolDependencyKey();

    public new CXElement CXNode { get; init; } = CXNode;

    private int? _dependencyKey;

    public static Result<FunctionalState> FromSymbol(
        IGraphContext context,
        ICSharpMethodSymbol symbol,
        GraphNode graphNode,
        CXElement element,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    )
    {
        if (!context.Renderer.IsValidComponentType(context, symbol.ReturnType, cancellationToken))
            return element.IdentifierTextSpanOrElementTextSpan.Report(
                Diagnostic.FunctionalComponentDoesntReturnAComponentType(symbol)
            );

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
                requiresValue: !parameter.Type.Equals(context.CompilationProvider.Boolean!)
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
            graphNode,
            element,
            symbol,
            [..properties],
            childrenParameter
        );

        if (childrenParameter is not null)
            state.SetPropertyValueToChildren(childrenParameter);

        return state;
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
           Parameters.Equals(other.Parameters) &&
           ChildrenParameter == other.ChildrenParameter &&
           base.Equals(other);

    public override int GetHashCode()
        => Hash.Combine(SymbolDependencyKey, Parameters, ChildrenParameter, base.GetHashCode());
}