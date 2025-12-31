using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed record InterleavedState(
    GraphNode GraphNode,
    ICXNode Source,
    int InterpolationId
) : ComponentState(GraphNode, Source)
{
    public bool Equals(InterleavedState? other)
    {
        if (other is null) return false;

        return
            InterpolationId == other.InterpolationId &&
            base.Equals(other);
    }

    public override int GetHashCode()
        => Hash.Combine(InterpolationId, base.GetHashCode());
}

public sealed class InterleavedComponentNode : ComponentNode<InterleavedState>, IDynamicComponentNode
{
    public ComponentBuilderKind Kind { get; }
    public ITypeSymbol Symbol { get; }

    public bool IsSingleCardinality
        => Kind == ComponentBuilderKind.IMessageComponentBuilder;

    public override string Name => "<interpolated component>";

    public InterleavedComponentNode(
        ComponentBuilderKind kind,
        ITypeSymbol symbol
    )
    {
        Kind = kind;
        Symbol = symbol;
    }

    public static bool TryCreate(
        ITypeSymbol? symbol,
        Compilation compilation,
        out InterleavedComponentNode node
    )
    {
        if (ComponentBuilderKind.IsValidComponentBuilderType(symbol, compilation, out var kind))
        {
            node = new(kind, symbol!);
            return true;
        }

        node = null!;
        return false;
    }

    public override InterleavedState? CreateState(ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics)
    {
        int id;

        switch (context.CXNode)
        {
            case CXValue.Interpolation interpolation:
                id = interpolation.Document!.GetInterpolationIndex(interpolation.Token);
                break;
            case CXToken { Kind: CXTokenKind.Interpolation } token:
                id = token.Document!.GetInterpolationIndex(token);
                break;
            default: return null;
        }

        return new InterleavedState(
            context.GraphNode,
            context.CXNode,
            id
        );
    }


    public override Result<string> Render(
        InterleavedState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        var designerValue = context.GetDesignerValue(
            state.InterpolationId,
            Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );

        var typingContext = options.TypingContext;

        if (typingContext is null)
        {
            if (state.IsRootNode)
            {
                typingContext = context.RootTypingContext;
            }
            else
            {
                /*
                 * TODO: unknown typing context may imply a bug where a parent component isn't supplying their
                 * required typing information
                 */

                Debug.Fail($"Unknown typing context in dynamic node: {state.GraphNode.ToPathString()}");
                typingContext = context.RootTypingContext;
            }
        }

        var value = ComponentBuilderKind.Convert(
            designerValue,
            Kind,
            typingContext.Value.ConformingType,
            typingContext.Value.CanSplat
        );

        if (value is null)
        {
            /*
             * we've failed to convert, this case implies that whatever the type of this interleaved node is, it doesn't
             * conform to the current constraints
             */

            return Result<string>.FromDiagnostic(
                Diagnostics.InvalidInterleavedComponentInCurrentContext(
                    Symbol.ToDisplayString(),
                    typingContext.Value.ConformingType.ToString()
                ),
                state.Source
            );
        }

        return value;
    }
}