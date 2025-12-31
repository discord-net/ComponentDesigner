using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class ContainerComponentNode : ComponentNode
{
    public override string Name => "container";

    public override bool HasChildren => true;

    public ComponentProperty Id { get; }
    public ComponentProperty AccentColor { get; }
    public ComponentProperty Spoiler { get; }

    public override ImmutableArray<ComponentProperty> Properties { get; }

    private static readonly ComponentRenderingOptions ChildRenderingOptions = new(
        TypingContext: new(
            CanSplat: true,
            ConformingType: ComponentBuilderKind.CollectionOfIMessageComponentBuilders
        )
    );

    public ContainerComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            AccentColor = new(
                "accentColor",
                isOptional: true,
                aliases: ["color", "accent"],
                renderer: CXValueGenerator.Color,
                dotnetPropertyName: "AccentColor"
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                requiresValue: false,
                renderer: CXValueGenerator.Boolean,
                dotnetPropertyName: "IsSpoiler"
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        foreach (var child in state.Children)
        {
            if (!IsValidChild(child.Inner))
            {
                diagnostics.Add(
                    Diagnostics.InvalidContainerChild(child.Inner.Name),
                    child.State.Source
                );
            }
        }

        base.Validate(state, context, diagnostics);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is IDynamicComponentNode
            or ActionRowComponentNode
            or TextDisplayComponentNode
            or SectionComponentNode
            or MediaGalleryComponentNode
            or SeparatorComponentNode
            or FileComponentNode;

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
                $"new {context.KnownTypes.ContainerBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(){
                    init.ToString()
                        .WithNewlinePadding(4)
                        .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                        .PostfixIfSome($"{Environment.NewLine}}}")
                }";
        })
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}