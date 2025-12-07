using System;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class SectionComponentNode : ComponentNode
{
    public override string Name => "section";

    public override bool HasChildren => true;

    public ComponentProperty Accessory { get; }
    public override IReadOnlyList<ComponentProperty> Properties { get; }

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
                renderer: Renderers.ComponentAsProperty(AccessoryRenderingOptions)
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context)
    {
        var accessoryPropertyValue = state.GetProperty(Accessory);
        
        if (!state.HasChildren && !accessoryPropertyValue.HasValue)
        {
            context.AddDiagnostic(
                Diagnostics.EmptySection,
                state.Source
            );

            base.Validate(state, context);
            return;
        }

        var accessoryCount = state.Children.Count(x => x.Inner is AccessoryComponentNode);
        var nonAccessoryCount = state.Children.Count - accessoryCount;

        if (accessoryPropertyValue.HasValue)
        {
            foreach (var accessory in state.Children.Where(x => x.Inner is AccessoryComponentNode))
            {
                context.AddDiagnostic(
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
                    context.AddDiagnostic(
                        Diagnostics.MissingAccessory,
                        state.Source
                    );
                    break;
                case > 1:
                    foreach (var accessory in state.Children.Where(x => x.Inner is AccessoryComponentNode).Skip(1))
                    {
                        context.AddDiagnostic(
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
                context.AddDiagnostic(
                    Diagnostics.MissingSectionChild,
                    state.Source
                );
                break;
            case > 3:
                foreach (var child in state.Children.Where(x => x.Inner is not AccessoryComponentNode).Skip(3))
                {
                    context.AddDiagnostic(
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
                context.AddDiagnostic(
                    Diagnostics.InvalidSectionChildComponentType,
                    child.State.Source,
                    child.Inner.Name
                );
            }
        }

        static bool IsValidChildType(ComponentNode node)
            => node is TextDisplayComponentNode or IDynamicComponentNode;
    }

    public override string Render(ComponentState state, IComponentContext context, ComponentRenderingOptions options)
    {
        var accessoryPropertyValue = state.GetProperty(Accessory);

        var renderedAccessory = accessoryPropertyValue.HasValue
            ? Accessory.Renderer(context, accessoryPropertyValue)
            : state
                .Children
                .FirstOrDefault(x => x.Inner is AccessoryComponentNode)
                ?.Render(context);

        return
            $"""
             new {context.KnownTypes.SectionBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                 accessory: {renderedAccessory?.WithNewlinePadding(4) ?? "null"},
                 components:
                 [
                     {
                         state
                             .RenderChildren(context, x => x.Inner is not AccessoryComponentNode)
                             .WithNewlinePadding(8)
                     }
                 ]
             ){state.RenderInitializer(this, context, x => x == Accessory).PrefixIfSome(Environment.NewLine)}
             """;
    }
}

public sealed class AccessoryComponentNode : ComponentNode
{
    public override string Name => "accessory";

    public override bool HasChildren => true;

    public override void Validate(ComponentState state, IComponentContext context)
    {
        if (!state.HasChildren)
        {
            context.AddDiagnostic(
                Diagnostics.EmptyAccessory,
                state.Source
            );

            base.Validate(state, context);
            return;
        }


        if (state.Children.Count is not 1)
        {
            var start = state.OwningNode!.Children[0].State.Source.Span.Start;
            var end = state.OwningNode!.Children[state.Children.Count - 1].State.Source.Span.End;

            context.AddDiagnostic(
                Diagnostics.TooManyAccessoryChildren,
                TextSpan.FromBounds(start, end)
            );

            base.Validate(state, context);
            return;
        }

        if (!IsAllowedChild(state.Children[0].Inner))
        {
            context.AddDiagnostic(
                Diagnostics.InvalidAccessoryChild,
                state.Children[0].State.Source,
                state.Children[0].Inner.Name
            );
        }

        base.Validate(state, context);
    }

    private static bool IsAllowedChild(ComponentNode node)
        => node is ButtonComponentNode or ThumbnailComponentNode;

    public override string Render(ComponentState state, IComponentContext context, ComponentRenderingOptions options)
        => state.RenderChildren(context);
}