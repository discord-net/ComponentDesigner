using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

using static ComponentBuilderKindUtils;

public sealed class InterleavedState : ComponentState
{
    public required int InterpolationId { get; init; }
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
        if (IsValidComponentBuilderType(symbol, compilation, out var kind))
        {
            node = new(kind, symbol!);
            return true;
        }

        node = null!;
        return false;
    }

    public override InterleavedState? CreateState(ComponentStateInitializationContext context)
    {
        int id;

        switch (context.Node)
        {
            case CXValue.Interpolation interpolation:
                id = interpolation.Document.GetInterpolationIndex(interpolation.Token);
                break;
            case CXToken { Kind: CXTokenKind.Interpolation } token:
                id = token.Document!.GetInterpolationIndex(token);
                break;
            default: return null;
        }

        return new InterleavedState()
        {
            InterpolationId = id,
            Source = context.Node
        };
    }


    public override string Render(InterleavedState state, IComponentContext context, ComponentRenderingOptions options)
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

                Debug.Fail("Unknown typing context in dynamic node");
                typingContext = context.RootTypingContext;
            }
        }

        var value = Convert(
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

            context.AddDiagnostic(
                Diagnostics.InvalidInterleavedComponentInCurrentContext,
                state.Source,
                Symbol.ToDisplayString(),
                typingContext.Value.ConformingType
            );
            
            return string.Empty;
        }

        return value;
    }
}