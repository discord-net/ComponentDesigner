using System;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.CX.Parser;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class SectionComponentNode : ComponentNode
{
    public override string Name => "section";

    public override bool HasChildren => true;

    public ComponentProperty Accessory { get; }
    public override ImmutableArray<ComponentProperty> Properties { get; }

    private static readonly ComponentRenderingOptions ChildrenRenderingOptions = new(
        TypingContext: new(
            CanSplat: true,
            ConformingType: ComponentBuilderKind.CollectionOfIMessageComponentBuilders
        )
    );

    private static readonly ComponentRenderingOptions AccessoryRenderingOptions = new(
        TypingContext: new(
            CanSplat: false,
            ConformingType: ComponentBuilderKind.IMessageComponentBuilder
        )
    );

    public SectionComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            Accessory = new(
                "accessory",
                isOptional: true,
                renderer: CXValueGenerator.Component
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        var accessoryPropertyValue = state.GetProperty(Accessory);

        if (!state.HasChildren && !accessoryPropertyValue.HasValue)
        {
            diagnostics.Add(
                Diagnostics.EmptySection,
                state.Source
            );

            base.Validate(state, context, diagnostics);
            return;
        }

        var accessoryCount = state.Children.Count(x => x.Inner is AccessoryComponentNode);
        var nonAccessoryCount = state.Children.Count - accessoryCount;

        if (accessoryPropertyValue.HasValue)
        {
            foreach (var accessory in state.Children.Where(x => x.Inner is AccessoryComponentNode))
            {
                diagnostics.Add(
                    Diagnostics.TooManyAccessories,
                    accessory.State.Source
                );
            }
        }
        else
        {
            switch (accessoryCount)
            {
                case 0:
                    diagnostics.Add(
                        Diagnostics.MissingAccessory,
                        state.Source
                    );
                    break;
                case > 1:
                    foreach (var accessory in state.Children.Where(x => x.Inner is AccessoryComponentNode).Skip(1))
                    {
                        diagnostics.Add(
                            Diagnostics.TooManyAccessories,
                            accessory.State.Source
                        );
                    }

                    break;
            }
        }

        switch (nonAccessoryCount)
        {
            case 0:
                diagnostics.Add(
                    Diagnostics.MissingSectionChild,
                    state.Source
                );
                break;
            case > 3:
                foreach (var child in state.Children.Where(x => x.Inner is not AccessoryComponentNode).Skip(3))
                {
                    diagnostics.Add(
                        Diagnostics.TooManySectionChildren,
                        child.State.Source
                    );
                }

                break;
        }

        foreach (var child in state.Children.Where(x => x.Inner is not AccessoryComponentNode))
        {
            if (!IsValidChildType(child.Inner))
            {
                diagnostics.Add(
                    Diagnostics.InvalidSectionChildComponentType(child.Inner.Name),
                    child.State.Source
                );
            }
        }

        static bool IsValidChildType(ComponentNode node)
            => node is TextDisplayComponentNode or IDynamicComponentNode;
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        var accessoryPropertyValue = state.GetProperty(Accessory);

        var renderedAccessory = (
            accessoryPropertyValue.HasValue
                ? Accessory.Renderer(
                    context,
                    new CXValueGeneratorTarget.ComponentProperty(accessoryPropertyValue),
                    AccessoryRenderingOptions
                )
                : (
                    state
                        .Children
                        .FirstOrDefault(x => x.Inner is AccessoryComponentNode)
                        ?.Render(context) ?? default
                )
        ).Or("null");

        return renderedAccessory
            .Combine(
                state.RenderChildren(context, x => x.Inner is not AccessoryComponentNode, ChildrenRenderingOptions))
            .Combine(state.RenderInitializer(this, context, x => x.Equals(Accessory)))
            .Map(x =>
                $"""
                 new {context.KnownTypes.SectionBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                     accessory: {x.Left.Left.WithNewlinePadding(4)},
                     components:
                     [
                         {x.Left.Right.WithNewlinePadding(8)}
                     ]
                 ){x.Right.PrefixIfSome(Environment.NewLine)}
                 """
            )
            .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
    }
}

public sealed class AccessoryComponentNode : ComponentNode
{
    public override string Name => "accessory";

    public override bool HasChildren => true;

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (!state.HasChildren)
        {
            diagnostics.Add(
                Diagnostics.EmptyAccessory,
                state.Source
            );

            base.Validate(state, context, diagnostics);
            return;
        }


        if (state.Children.Count is not 1)
        {
            var start = state.GraphNode!.Children[0].State.Source.Span.Start;
            var end = state.GraphNode!.Children[state.Children.Count - 1].State.Source.Span.End;

            diagnostics.Add(
                Diagnostics.TooManyAccessoryChildren,
                TextSpan.FromBounds(start, end)
            );

            base.Validate(state, context, diagnostics);
            return;
        }

        if (!IsAllowedChild(state.Children[0].Inner))
        {
            diagnostics.Add(
                Diagnostics.InvalidAccessoryChild(state.Children[0].Inner.Name),
                state.Children[0].State.Source
            );
        }

        base.Validate(state, context, diagnostics);
    }

    private static bool IsAllowedChild(ComponentNode node)
        => node is ButtonComponentNode or ThumbnailComponentNode;

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state.RenderChildren(context);
}