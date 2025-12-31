using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord.CX.Parser;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components.Custom;

public sealed record FunctionalComponentState(
    GraphNode GraphNode,
    CXElement Source,
    string Identifier,
    string MethodReference,
    EquatableArray<ComponentProperty> Properties,
    int PropertiesTypeKey,
    ComponentBuilderKind ReturnKind,
    ComponentProperty? ChildrenParameter,
    ComponentBuilderKind? ChildrenComponentKind
) : ComponentState(GraphNode, Source)
{
    [MemberNotNullWhen(true, nameof(ChildrenParameter), nameof(ChildrenComponentKind))]
    public bool HasComponentChildren
        => ChildrenParameter is not null && ChildrenComponentKind is not null and not ComponentBuilderKind.None;

    public new CXElement Source { get; init; } = Source;

    public bool Equals(FunctionalComponentState? other)
    {
        if (other is null) return false;

        return
            PropertiesTypeKey == other.PropertiesTypeKey &&
            Identifier == other.Identifier &&
            MethodReference == other.MethodReference &&
            Properties.Equals(other.Properties) &&
            ReturnKind == other.ReturnKind &&
            (ChildrenParameter?.Equals(other.ChildrenParameter) ?? other.ChildrenParameter is null) &&
            ChildrenComponentKind == other.ChildrenComponentKind &&
            base.Equals(other);
    }


    public override int GetHashCode()
        => Hash.Combine(
            PropertiesTypeKey,
            Identifier,
            MethodReference,
            Properties,
            ReturnKind,
            ChildrenParameter,
            ChildrenComponentKind,
            base.GetHashCode()
        );
}

public sealed class FunctionalComponentNode :
    ComponentNode<FunctionalComponentState>,
    IDynamicComponentNode
{
    public static readonly FunctionalComponentNode Instance = new();

    protected override bool IsUserAccessible => false;

    public override string Name => "<functional component>";

    public override void AddGraphNode(ComponentGraphInitializationContext context)
    {
        context.Push(
            this,
            cxNode: context.CXNode
        );
    }

    public override FunctionalComponentState? CreateState(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var method = SearchForTarget(element.Identifier, context.CXNode, context.GraphContext, diagnostics);

        if (method is null) return null;

        var state = CreateFromSymbol(context.Compilation, method, context.GraphNode, element, diagnostics);

        if (state?.ChildrenParameter is not null)
            context.AddChildren(element.Children.OfType<CXElement>());

        return state;
    }

    public override FunctionalComponentState UpdateState(
        FunctionalComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        /*
         * returning old state is fine in cases that we fail to generate it, as produced diagnostics will cause
         * the rendering pipeline to end
         */
        var method = SearchForTarget(state.Identifier, state.Source, context, diagnostics);

        if (method is null) return state;

        return CreateFromSymbol(
            context.Compilation,
            method,
            state.GraphNode,
            state.Source,
            diagnostics
        ) ?? state;
    }

    private readonly record struct SearchResult(
        ISymbol Symbol,
        SearchResultKind Kind
    );

    private enum SearchResultKind
    {
        Ok,

        NonPublic,
        NonStatic,
        InvalidComponentReturnKind,
        NotAMethod
    }

    private static IMethodSymbol? SearchForTarget(
        string name,
        ICXNode node,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var candidates = context.CX.SemanticModel.LookupSymbols(
            context.CX.Location.TextSpan.Start,
            name: name
        );

        var results = new List<SearchResult>();

        foreach (var candidate in candidates)
        {
            if (candidate is not IMethodSymbol method)
            {
                results.Add(new(candidate, SearchResultKind.NotAMethod));
                continue;
            }

            if (!ComponentBuilderKind.IsValidComponentBuilderType(method.ReturnType, context.Compilation))
            {
                results.Add(new(candidate, SearchResultKind.InvalidComponentReturnKind));
                continue;
            }

            if (!method.IsStatic)
            {
                results.Add(new(candidate, SearchResultKind.NonStatic));
                continue;
            }

            if (method.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
            {
                results.Add(new(candidate, SearchResultKind.NonPublic));
                continue;
            }

            results.Add(new(candidate, SearchResultKind.Ok));
        }

        if (results.Count is 0)
        {
            diagnostics.Add(
                Diagnostics.UnknownComponent(name),
                node
            );
            return null;
        }

        if (results.Count is 1)
        {
            return Single(results[0]);
        }

        // find the best kind
        var group = results
            .GroupBy(x => x.Kind)
            .OrderBy(x => x.Key)
            .First();

        var targets = group.ToArray();

        if (targets.Length is 1)
        {
            return Single(targets[0]);
        }

        diagnostics.Add(
            Diagnostics.AmbiguousFunctionalComponent(
                targets.Select(x => x.Symbol.ToDisplayString())
            ),
            node
        );

        return null;


        IMethodSymbol? Single(SearchResult result)
        {
            if (result.Kind is SearchResultKind.Ok) return result.Symbol as IMethodSymbol;

            diagnostics.Add(
                Diagnostics.InvalidFunctionalComponentKind(
                    result.Symbol.ToDisplayString(),
                    result.Kind switch
                    {
                        SearchResultKind.NotAMethod => "not a method",
                        SearchResultKind.InvalidComponentReturnKind => "invalid component return type",
                        SearchResultKind.NonPublic => "not public or internal",
                        SearchResultKind.NonStatic => "not state",
                        _ => "unknown"
                    }
                ),
                node
            );

            return null;
        }
    }

    private static FunctionalComponentState? CreateFromSymbol(
        Compilation compilation,
        IMethodSymbol methodSymbol,
        GraphNode graphNode,
        CXElement source,
        IList<DiagnosticInfo> diagnostics
    )
    {
        if (
            !ComponentBuilderKind.IsValidComponentBuilderType(
                methodSymbol.ReturnType,
                compilation,
                out var returnKind
            )
        )
        {
            diagnostics.Add(
                Diagnostics.InvalidFunctionalComponentReturnType(
                    methodSymbol.Name,
                    methodSymbol.ReturnType.ToDisplayString()
                ),
                source
            );

            return null;
        }

        var properties = new List<ComponentProperty>();
        var key = 0;

        ComponentProperty? childrenParameter = null;
        CXValue? substituteChildValue = null;
        ComponentBuilderKind? childrenParameterKind = null;

        for (var i = 0; i < methodSymbol.Parameters.Length; i++)
        {
            var parameter = methodSymbol.Parameters[i];

            key = Hash.Combine(key, parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            var childParameterAttribute = parameter
                .GetAttributes()
                .FirstOrDefault(x =>
                    compilation
                        .GetKnownTypes()
                        .CXChildrenAttribute!
                        .Equals(x.AttributeClass, SymbolEqualityComparer.Default)
                );

            CXValueGeneratorDelegate renderer;

            if (childParameterAttribute is not null && childrenParameter is null)
            {
                renderer = CreateChildrenRenderer(
                    compilation,
                    parameter.Type,
                    source,
                    diagnostics,
                    out childrenParameterKind,
                    out substituteChildValue
                );
            }
            else
            {
                renderer = CXValueGenerator.GetGeneratorForType(
                    compilation,
                    parameter.Type
                );
            }

            var property = new ComponentProperty(
                parameter.Name,
                isOptional: parameter.HasExplicitDefaultValue,
                renderer: renderer
            );

            if (childParameterAttribute is not null)
            {
                if (childrenParameter is not null)
                {
                    diagnostics.Add(
                        Diagnostics.DuplicateChildParameter(methodSymbol.Name),
                        source
                    );
                }

                childrenParameter = property;
            }

            properties.Add(property);
        }

        var state = new FunctionalComponentState(
            graphNode,
            source,
            source.Identifier,
            $"{methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{methodSymbol.Name}",
            [..properties],
            key,
            returnKind,
            childrenParameter,
            childrenParameterKind
        );

        if (substituteChildValue is not null && childrenParameter is not null)
            state.SubstitutePropertyValue(childrenParameter, substituteChildValue);

        return state;
    }

    private static CXValueGeneratorDelegate CreateChildrenRenderer(
        Compilation compilation,
        ITypeSymbol childrenType,
        CXElement source,
        IList<DiagnosticInfo> diagnostics,
        out ComponentBuilderKind? childrenKind,
        out CXValue? childValue
    )
    {
        childValue = null;

        if (ComponentBuilderKind.IsValidComponentBuilderType(childrenType, compilation, out var kind))
        {
            childrenKind = kind;
            return CXValueGenerator.Default;
        }

        childrenKind = null;

        // extract a single value, if any
        if (source.Children.Count is 0) return CXValueGenerator.Default;

        if (source.Children[0] is not CXValue value)
        {
            diagnostics.Add(
                Diagnostics.ExpectedScalarFunctionalComponentChildValue,
                source.Children[0]
            );

            return CXValueGenerator.Default;
        }

        childValue = value;

        // add any remaining children as errors
        if (source.Children.Count > 1)
        {
            var lower = source.Children[1].Span.Start;
            var upper = source.Children[source.Children.Count - 1].Span.End;

            diagnostics.Add(
                Diagnostics.ExpectedScalarFunctionalComponentChildValue,
                TextSpan.FromBounds(lower, upper)
            );
        }

        return CXValueGenerator.GetGeneratorForType(compilation, childrenType);
    }

    private static Result<string> RenderElementChildren(
        FunctionalComponentState state,
        IComponentContext context,
        CXValueGeneratorOptions options
    )
    {
        if (state.Source.Children.Count is 0 || state.ChildrenComponentKind is null or ComponentBuilderKind.None)
            return string.Empty;

        var graphIndex = 0;

        var renderers = new List<BoundComponentNodeRenderer>();
        var nodes = new List<ICXNode>();

        foreach (var child in state.Source.Children)
        {
            switch (child)
            {
                case CXElement element:
                {
                    var index = graphIndex++;
                    var graphNode = state.GraphNode.Children.ElementAtOrDefault(index);

                    if (graphNode is null)
                    {
                        // diagnostics should come from that nodes attempt to create state, we can just bail
                        return Result<string>.Empty;
                    }

                    renderers.Add(graphNode.Render);
                    nodes.Add(element);
                    break;
                }
                case CXValue.Interpolation interpolation:
                {
                    renderers.Add(Interpolation(interpolation, context.GetInterpolationInfo(interpolation)));
                    nodes.Add(interpolation);
                    break;
                }
                case CXValue.Multipart multipart:
                {
                    foreach (var token in multipart.Tokens)
                    {
                        nodes.Add(token);

                        if (token.InterpolationIndex is { } index)
                        {
                            renderers.Add(Interpolation(token, context.GetInterpolationInfo(index)));
                        }
                        else
                        {
                            renderers.Add((componentContext, renderingOptions) => new DiagnosticInfo(
                                    Diagnostics.TypeMismatch("component", token.Kind.ToString()),
                                    token
                                )
                            );
                        }
                    }

                    break;
                }
                default:
                    renderers.Add((componentContext, renderingOptions) => new DiagnosticInfo(
                            Diagnostics.TypeMismatch("component", child.GetType().Name),
                            child
                        )
                    );
                    nodes.Add(child);
                    break;
            }
        }

        return CreateChildMapper(state.ChildrenComponentKind.Value)(
            state,
            context,
            state.ChildrenParameter!,
            renderers,
            nodes
        );

        static BoundComponentNodeRenderer Interpolation(ICXNode node, DesignerInterpolationInfo info)
            => (context, options) =>
            {
                if (
                    !ComponentBuilderKind.IsValidComponentBuilderType(
                        info.Symbol,
                        context.Compilation,
                        out var kind
                    )
                )
                {
                    return new DiagnosticInfo(
                        Diagnostics.TypeMismatch("component", info.Symbol!.ToDisplayString()),
                        node
                    );
                }

                var source = context.GetDesignerValue(info, info.Symbol!.ToDisplayString());

                return kind.Conform(
                    source,
                    options.TypingContext ?? context.RootTypingContext,
                    node
                );
            };
    }

    private delegate Result<string> ChildMapper(
        FunctionalComponentState state,
        IComponentContext context,
        ComponentProperty property,
        IReadOnlyList<BoundComponentNodeRenderer> renderers,
        IReadOnlyList<ICXNode> nodes
    );

    private static ChildMapper CreateChildMapper(
        ComponentBuilderKind kind
    )
    {
        switch (kind)
        {
            case ComponentBuilderKind.IMessageComponentBuilder:
            case ComponentBuilderKind.IMessageComponent:
                return (state, context, property, renderers, nodes) =>
                    renderers.Count switch
                    {
                        0 => property.IsOptional
                            ? "default"
                            : new DiagnosticInfo(
                                Diagnostics.MissingRequiredProperty(state.MethodReference, property.Name),
                                state.Source
                            ),
                        1 => renderers[0](context, new(new(
                            CanSplat: false,
                            kind
                        ))),
                        _ => new DiagnosticInfo(
                            Diagnostics.InvalidChildComponentCardinality(state.Identifier),
                            TextSpan.FromBounds(nodes[0].Span.Start, nodes[nodes.Count - 1].Span.End)
                        )
                    };
            case ComponentBuilderKind.CXMessageComponent:
            case ComponentBuilderKind.MessageComponent:
                return (state, context, property, renderers, nodes) =>
                    renderers.Count switch
                    {
                        0 => property.IsOptional
                            ? "global::Discord.CXMessageComponent.Empty"
                            : new DiagnosticInfo(
                                Diagnostics.MissingRequiredProperty(state.MethodReference, property.Name),
                                state.Source
                            ),
                        _ => renderers
                            .Select(x => x(context, new(new(
                                CanSplat: true,
                                ComponentBuilderKind.CollectionOfIMessageComponentBuilders
                            ))))
                            .FlattenAll()
                            .Map(x =>
                                $"""
                                 new global::Discord.CXMessageComponent([
                                     {string.Join($",{Environment.NewLine}", x).WithNewlinePadding(4)}
                                 ])
                                 """
                            )
                    };
            case ComponentBuilderKind.CollectionOfIMessageComponentBuilders:
            case ComponentBuilderKind.CollectionOfIMessageComponents:
            case ComponentBuilderKind.CollectionOfCXComponents:
            case ComponentBuilderKind.CollectionOfMessageComponents:
                return (state, context, property, renderers, nodes) =>
                    renderers.Count switch
                    {
                        0 => property.IsOptional
                            ? "[]"
                            : new DiagnosticInfo(
                                Diagnostics.MissingRequiredProperty(state.MethodReference, property.Name),
                                state.Source
                            ),
                        _ => renderers
                            .Select(x => x(context, new(new(
                                CanSplat: true,
                                kind
                            ))))
                            .FlattenAll()
                            .Map(x =>
                                $"""
                                 [
                                    {string.Join($",{Environment.NewLine}", x).WithNewlinePadding(4)}
                                 ]
                                 """
                            )
                    };
            default:
                return (state, context, property, renderers, nodes) => new DiagnosticInfo(
                    Diagnostics.TypeMismatch(kind.ToString(), "unknown component"),
                    nodes.Count is 0
                        ? state.Source.Span
                        : TextSpan.FromBounds(nodes[0].Span.Start, nodes[nodes.Count - 1].Span.End)
                );
        }
    }

    public override void Validate(
        FunctionalComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        ValidateProperties(
            state,
            state.Properties,
            context,
            diagnostics,
            ignorePredicate: state.HasComponentChildren
                ? prop => prop == state.ChildrenParameter
                : null
        );
        ValidateChildren(
            state,
            context,
            diagnostics,
            allowsChildrenInCX: state.ChildrenParameter is not null,
            hasChildren: state.HasComponentChildren
        );
    }

    public override Result<string> Render(
        FunctionalComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(
            state.Properties,
            context,
            ignorePredicate: state.ChildrenParameter is not null && state.ChildrenComponentKind is not null
                ? x => x.Equals(state.ChildrenParameter)
                : null
        )
        .Combine(
            state.HasComponentChildren
                ? RenderElementChildren(state, context, options)
                    .Map(x => $"{state.ChildrenParameter.Name}: {x}")
                : string.Empty
        )
        .Map(tuple =>
        {
            var props = tuple.Left;

            if (!string.IsNullOrWhiteSpace(tuple.Right))
            {
                if (!string.IsNullOrWhiteSpace(props))
                    props += $",{Environment.NewLine}";

                props += tuple.Right;
            }

            return state.ReturnKind.Conform(
                $"{state.MethodReference}({
                    props.WithNewlinePadding(4).PrefixIfSome(4).WrapIfSome(Environment.NewLine)
                })",
                options.TypingContext ?? context.RootTypingContext,
                state.Source
            );
        });
}