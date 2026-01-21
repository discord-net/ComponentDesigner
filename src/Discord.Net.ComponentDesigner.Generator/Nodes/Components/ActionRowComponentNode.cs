using System;
using Discord.CX.Parser;
using Discord.CX.Nodes.Components.SelectMenus;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    public override ImmutableArray<ComponentProperty> Properties { get; }

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

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (!context.IsMessageContext)
        {
            diagnostics.Add(
                Diagnostics.ComponentNotAllowedInContext(Name, context.CX.Kind),
                state.Source
            );
            return;
        }
            
        if (!state.HasChildren)
        {
            diagnostics.Add(
                Diagnostics.EmptyActionRow,
                state.Source
            );

            base.Validate(state, context, diagnostics);
            return;
        }

        switch (state.Children[0].Inner)
        {
            case ButtonComponentNode:
                foreach (var rest in state.Children.Skip(1))
                {
                    if (rest.Inner is not ButtonComponentNode)
                    {
                        diagnostics.Add(
                            Diagnostics.ActionRowInvalidChild,
                            rest.State.Source
                        );
                    }
                }

                foreach (var extra in state.Children.Skip(5))
                {
                    diagnostics.Add(
                        Diagnostics.ActionRowInvalidChild,
                        extra.State.Source
                    );
                }

                break;
            case SelectMenuComponentNode:
                foreach (var rest in state.Children.Skip(1))
                {
                    diagnostics.Add(
                        Diagnostics.ActionRowInvalidChild,
                        rest.State.Source
                    );
                }

                break;

            case IDynamicComponentNode: break;

            default:
                foreach (
                    var rest
                    in state.Children.Where(x => !IsValidChild(x.Inner))
                )
                {
                    diagnostics.Add(
                        Diagnostics.ActionRowInvalidChild,
                        rest.State.Source
                    );
                }

                break;
        }

        base.Validate(state, context, diagnostics);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is ButtonComponentNode
            or SelectMenuComponentNode
            or IDynamicComponentNode;

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context, asInitializers: true)
        .Combine(state.RenderChildren(context, options: ChildRenderingOptions))
        .Map(x =>
        {
            var (props, children) = x;

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
                $"new {context.KnownTypes.ActionRowBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(){
                    init.ToString()
                        .WithNewlinePadding(4)
                        .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                        .PostfixIfSome($"{Environment.NewLine}}}")
                }";
        })
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}