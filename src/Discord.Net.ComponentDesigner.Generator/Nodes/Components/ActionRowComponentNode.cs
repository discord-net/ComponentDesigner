using System;
using Discord.CX.Parser;
using Discord.CX.Nodes.Components.SelectMenus;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public class ActionRowComponentNode : ComponentNode
{
    public override string Name => "action-row";

    public override IReadOnlyList<string> Aliases { get; } = ["row"];

    public override bool HasChildren => true;

    public ComponentProperty Id { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    private static readonly ComponentRenderingOptions ChildRenderingOptions = new(
        TypingContext: new(
            CanSplat: true,
            ConformingType: ComponentBuilderKind.CollectionOfIMessageComponentBuilders
        )
    );

    public ActionRowComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context)
    {
        if (!state.HasChildren)
        {
            context.AddDiagnostic(
                Diagnostics.EmptyActionRow,
                state.Source
            );

            base.Validate(state, context);
            return;
        }

        switch (state.Children[0].Inner)
        {
            case ButtonComponentNode:
                foreach (var rest in state.Children.Skip(1))
                {
                    if (rest.Inner is not ButtonComponentNode)
                    {
                        context.AddDiagnostic(
                            Diagnostics.ActionRowInvalidChild,
                            rest.State.Source
                        );
                    }
                }

                foreach (var extra in state.Children.Skip(5))
                {
                    context.AddDiagnostic(
                        Diagnostics.ActionRowInvalidChild,
                        extra.State.Source
                    );
                }

                break;
            case SelectMenuComponentNode:
                foreach (var rest in state.Children.Skip(1))
                {
                    context.AddDiagnostic(
                        Diagnostics.ActionRowInvalidChild,
                        rest.State.Source
                    );
                }

                break;

            case InterleavedComponentNode: break;

            default:
                foreach (
                    var rest
                    in state.Children.Where(x => !IsValidChild(x.Inner))
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.ActionRowInvalidChild,
                        rest.State.Source
                    );
                }

                break;
        }

        base.Validate(state, context);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is ButtonComponentNode
            or SelectMenuComponentNode
            or IDynamicComponentNode;

    public override string Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        var props = state.RenderProperties(this, context, asInitializers: true);
        var children = state.RenderChildren(context, options: ChildRenderingOptions);

        var init = new StringBuilder(props);

        if (!string.IsNullOrWhiteSpace(children))
        {
            if (!string.IsNullOrWhiteSpace(props)) init.Append(',').AppendLine();

            init.Append(
                $"""
                 Components =
                 [
                     {children.WithNewlinePadding(4)}
                 ]
                 """
            );
        }

        return
            $$"""
              new {{context.KnownTypes.ActionRowBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(){{
                  init
                      .ToString()
                      .WithNewlinePadding(4)
                      .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                      .PostfixIfSome($"{Environment.NewLine}}}")
              }}
              """;
    }
}

public sealed class AutoActionRowComponentNode : ActionRowComponentNode
{
    public static readonly AutoActionRowComponentNode Instance = new();
    protected override bool IsUserAccessible => false;

    public override ComponentState? Create(ComponentStateInitializationContext context)
    {
        return new ComponentState() { Source = context.Node };
    }

    public override void Validate(ComponentState state, IComponentContext context)
    {
        // no validation occurs for auto rows
    }
}