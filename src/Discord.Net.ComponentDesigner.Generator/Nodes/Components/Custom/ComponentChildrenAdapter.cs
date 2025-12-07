using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components.Custom;

public sealed class ComponentChildrenAdapter
{
    public delegate string Renderer(
        IComponentContext context,
        ComponentState state,
        IReadOnlyList<ComponentChild> children
    );

    public Renderer ChildrenRenderer { get; }
    
    public bool IsOptional { get; }

    public bool IsCollectionType
        => _collectionInnerTypeSymbol is not null;

    public bool IsCX => _componentBuilderKind is not ComponentBuilderKind.None;

    private readonly ITypeSymbol _targetSymbol;
    private readonly ITypeSymbol? _collectionInnerTypeSymbol;
    private readonly ComponentBuilderKind _componentBuilderKind;
    private readonly ComponentNode _owner;

    public abstract record ComponentChild(ICXNode Node)
    {
        // any text literal
        public sealed record Text(CXToken Token) : ComponentChild(Token);

        // an interpolation
        public sealed record Interpolation(CXToken Token) : ComponentChild(Token);

        // another element
        public sealed record Element(CXElement CX) : ComponentChild(CX);
    }

    public ComponentChildrenAdapter(
        ITypeSymbol targetSymbol,
        ITypeSymbol? collectionInnerTypeSymbol,
        ComponentBuilderKind componentBuilderKind,
        bool isOptional,
        ComponentNode owner
    )
    {
        IsOptional = isOptional;
        _targetSymbol = targetSymbol;
        _collectionInnerTypeSymbol = collectionInnerTypeSymbol;
        _componentBuilderKind = componentBuilderKind;
        _owner = owner;

        ChildrenRenderer = componentBuilderKind is not ComponentBuilderKind.None
            ? RenderComponent
            : RenderNonComponent;
    }

    public static ComponentChildrenAdapter Create(
        Compilation compilation,
        ITypeSymbol target,
        bool isOptional,
        ComponentNode owner
    )
    {
        var inner = target
            .AllInterfaces
            .FirstOrDefault(x =>
                x.IsGenericType &&
                x.ConstructedFrom.Equals(
                    compilation.GetKnownTypes().IEnumerableOfTType,
                    SymbolEqualityComparer.Default
                )
            )
            ?.TypeArguments[0];

        ComponentBuilderKindUtils.IsValidComponentBuilderType(inner ?? target, compilation, out var kind);

        return new(
            target,
            inner,
            kind,
            isOptional,
            owner
        );
    }

    public IReadOnlyList<ComponentChild> AdaptToState(ComponentStateInitializationContext context)
    {
        if (context.Node is not CXElement element) return [];

        var children = new List<ComponentChild>();

        foreach (var child in element.Children)
        {
            switch (child)
            {
                case CXValue.Multipart multipart and not CXValue.StringLiteral:
                    foreach (var part in multipart.Tokens)
                    {
                        switch (part.Kind)
                        {
                            case CXTokenKind.Interpolation:
                                children.Add(new ComponentChild.Interpolation(part));
                                break;
                            case CXTokenKind.Text:
                                children.Add(new ComponentChild.Text(part));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(part));
                        }
                    }

                    break;
                case CXValue.Interpolation interpolation:
                    children.Add(new ComponentChild.Interpolation(interpolation.Token));
                    break;
                case CXValue.Scalar scalar:
                    children.Add(new ComponentChild.Text(scalar.Token));
                    break;
                case CXElement childElement:
                    children.Add(new ComponentChild.Element(childElement));
                    context.AddChildren(childElement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(child));
            }
        }

        return children;
    }

    private string RenderComponent(
        IComponentContext context,
        ComponentState state,
        IReadOnlyList<ComponentChild> children
    )
    {
        if (_componentBuilderKind is ComponentBuilderKind.None) return string.Empty;

        var cardinalityOfMany = IsCollectionType || _componentBuilderKind.SupportsCardinalityOfMany();

        if (!cardinalityOfMany)
        {
            // expect one child
            switch (children.Count)
            {
                case 0:
                    if (IsOptional) return "default";

                    context.AddDiagnostic(
                        Diagnostics.MissingRequiredProperty,
                        state.Source,
                        _owner.Name,
                        "children"
                    );
                    return string.Empty;
                case 1:
                    return RenderComponentInner(_componentBuilderKind, context, children[0], state);
                default:
                    // too many children
                    var lower = children[1].Node.Span.Start;
                    var upper = children.Last().Node.Span.End;

                    context.AddDiagnostic(
                        Diagnostics.InvalidChildComponentCardinality,
                        TextSpan.FromBounds(lower, upper),
                        "children"
                    );
                    return string.Empty;
            }
        }

        if (IsCollectionType)
        {
            if (children.Count is 0)
            {
                if (IsOptional) return "[]";

                context.AddDiagnostic(
                    Diagnostics.MissingRequiredProperty,
                    state.Source,
                    _owner.Name,
                    "children"
                );
                return string.Empty;
            }

            return RenderChildrenAsCollection(_componentBuilderKind);
        }
        else
        {
            /*
             * A non-collection, cardinality of many type.
             * There are 2 cases for this point, either a 'MessageComponent' or 'CXMessageComponent'.
             * For construction, they both take a collection of either builder interfaces or component
             * interfaces, so we'll need to convert the children to one of those types.
             *
             * Since CX renders to the builders, we'll do the same, and wrap the construction manually
             */

            var typeName = _componentBuilderKind switch
            {
                ComponentBuilderKind.CXMessageComponent => "global::Discord.CXMessageComponent",
                ComponentBuilderKind.MessageComponent => "global::Discord.MessageComponent",
                _ => throw new ArgumentOutOfRangeException(nameof(_componentBuilderKind))
            };

            if (children.Count is 0)
            {
                if (IsOptional) return $"{typeName}.Empty";

                context.AddDiagnostic(
                    Diagnostics.MissingRequiredProperty,
                    state.Source,
                    _owner.Name,
                    "children"
                );
                return string.Empty;
            }

            var components = RenderChildrenAsCollection(ComponentBuilderKind.IMessageComponentBuilder);

            if (components == string.Empty) return string.Empty;

            return $"new {typeName}({components})";
        }

        string RenderChildrenAsCollection(ComponentBuilderKind kind)
        {
            var parts = new List<string>();

            foreach (var child in children)
            {
                var part = RenderComponentInner(
                    kind,
                    context,
                    child,
                    state,
                    spreadCollections: true
                );

                if (part == string.Empty) return string.Empty;
                parts.Add(part);
            }

            return $"[{
                string
                    .Join(
                        ",\n",
                        parts.Select(x => x
                            .Prefix(4)
                            .WithNewlinePadding(4)
                        )
                    )
                    .WrapIfSome("\n")
            }]";
        }
    }

    private string RenderComponentInner(
        ComponentBuilderKind kind,
        IComponentContext context,
        ComponentChild child,
        ComponentState state,
        bool spreadCollections = false
    )
    {
        /*
         * 'target' is guaranteed to be one of
         *  - CXMessageComponent
         *  - IMessageComponentBuilder
         *  - IMessageComponent
         *  - MessageComponent
         */

        switch (child)
        {
            case ComponentChild.Interpolation(var token):
                var info = context.GetInterpolationInfo(token);

                if (
                    !ComponentBuilderKindUtils.IsValidComponentBuilderType(
                        info.Symbol,
                        context.Compilation,
                        out var interpolationKind
                    ) ||
                    !ComponentBuilderKindUtils.TryConvert(
                        context.GetDesignerValue(
                            info,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        ),
                        interpolationKind,
                        kind,
                        out var converted,
                        spreadCollections
                    )
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        token,
                        info.Symbol!.ToDisplayString(),
                        kind
                    );

                    return string.Empty;
                }

                return converted;
            case ComponentChild.Element(var element):
                if (!state.TryGetChildGraphNode(element, out var childNode))
                {
                    // can happen if the child node wasn't valid in the graph, we can just not render
                    return string.Empty;
                }

                var builder = childNode.Render(context);

                if (
                    !ComponentBuilderKindUtils.TryConvert(
                        builder,
                        ComponentBuilderKind.IMessageComponentBuilder,
                        kind,
                        out converted,
                        spreadCollections
                    )
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        element,
                        "CX",
                        kind
                    );
                    return string.Empty;
                }

                return converted;
            case ComponentChild.Text(var token):
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    token,
                    "text",
                    kind
                );
                return string.Empty;
            default: throw new ArgumentOutOfRangeException(nameof(child));
        }
    }

    private string RenderNonComponent(
        IComponentContext context,
        ComponentState state,
        IReadOnlyList<ComponentChild> children
    )
    {
        if (!IsCollectionType)
        {
            // expect only 1 child
            switch (children.Count)
            {
                case 0:
                    if (IsOptional) return "default";

                    context.AddDiagnostic(
                        Diagnostics.MissingRequiredProperty,
                        state.Source,
                        _owner.Name,
                        "children"
                    );
                    return string.Empty;
                case 1:
                    return RenderNonComponentInner(_targetSymbol, context, children[0]);
                default:
                    // too many children
                    var lower = children[1].Node.Span.Start;
                    var upper = children.Last().Node.Span.End;

                    context.AddDiagnostic(
                        Diagnostics.InvalidChildComponentCardinality,
                        TextSpan.FromBounds(lower, upper),
                        "children"
                    );
                    return string.Empty;
            }
        }

        // should not be null if it's a collection
        if (_collectionInnerTypeSymbol is null) return string.Empty;

        if (children.Count is 0)
        {
            if (IsOptional) return "[]";

            context.AddDiagnostic(
                Diagnostics.MissingRequiredProperty,
                state.Source,
                _owner.Name,
                "children"
            );
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var child in children)
        {
            if (child is ComponentChild.Interpolation(var token))
            {
                var info = context.GetInterpolationInfo(token);
                // check if we can spread

                var enumerableType = info
                    .Symbol
                    ?.AllInterfaces
                    .FirstOrDefault(x =>
                        x.IsGenericType &&
                        x.ConstructedFrom.Equals(
                            context.KnownTypes.IEnumerableOfTType,
                            SymbolEqualityComparer.Default
                        )
                    );

                if (enumerableType is not null)
                {
                    if (
                        !context.Compilation.HasImplicitConversion(
                            enumerableType.TypeArguments[0],
                            _collectionInnerTypeSymbol
                        )
                    )
                    {
                        // cant spread, element types don't match
                        context.AddDiagnostic(
                            Diagnostics.TypeMismatch,
                            token,
                            enumerableType.TypeArguments[0].ToDisplayString(),
                            _collectionInnerTypeSymbol.ToDisplayString()
                        );

                        return string.Empty;
                    }

                    parts.Add(
                        $"..{
                            context.GetDesignerValue(
                                info,
                                enumerableType
                                    .TypeArguments[0]
                                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        }"
                    );
                    continue;
                }
            }

            var part = RenderNonComponentInner(_collectionInnerTypeSymbol, context, child);

            if (part == string.Empty) return string.Empty;

            parts.Add(part);
        }

        return $"[{
            string
                .Join(
                    ",\n",
                    parts.Select(x => x
                        .Prefix(4)
                        .WithNewlinePadding(4)
                    )
                )
                .WrapIfSome("\n")
        }]";
    }

    private string RenderNonComponentInner(
        ITypeSymbol target,
        IComponentContext context,
        ComponentChild child
    )
    {
        switch (child)
        {
            case ComponentChild.Element(var element):
                // TODO: maybe check for conversion from message builder to the target?
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    element,
                    "CX",
                    target.ToDisplayString()
                );
                return string.Empty;
            case ComponentChild.Interpolation(var token):
                // validate the type
                var info = context.GetInterpolationInfo(token);
                if (
                    !context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        target
                    )
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        token,
                        info.Symbol!.ToDisplayString(),
                        target.ToDisplayString()
                    );
                }

                // return out the designer value
                return context.GetDesignerValue(info, target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            case ComponentChild.Text(var token):
                // TODO: we could make use of the renderers for this, ex parsing colors, ints, etc
                // for now, check if strings are allowed
                // validate the type
                if (
                    !context.Compilation.HasImplicitConversion(
                        context.Compilation.GetKnownTypes().StringType,
                        target
                    )
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        token,
                        context.Compilation.GetKnownTypes().StringType,
                        target.ToDisplayString()
                    );
                }

                return Renderers.ToCSharpString(token.Value);
            default:
                throw new ArgumentOutOfRangeException(nameof(child));
        }
    }

    // public static ComponentChildrenAdapter Create(
    //     CXElement element,
    //     
    //     )
    // {

    // }
}