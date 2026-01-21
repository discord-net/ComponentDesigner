using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class FileComponentNode : ComponentNode
{
    public override string Name => "file";

    protected override bool AllowChildrenInCX => false;

    public ComponentProperty Id { get; }
    public ComponentProperty Url { get; }
    public ComponentProperty Spoiler { get; }

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public FileComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Url = new(
                "url",
                aliases: ["media"],
                renderer: CXValueGenerator.UnfurledMediaItem,
                dotnetParameterName: "media"
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                renderer: CXValueGenerator.NullableBoolean,
                dotnetParameterName: "isSpoiler"
            )
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
        
        base.Validate(state, context, diagnostics);
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state.RenderProperties(this, context)
        .Map(x =>
            $"new {context.KnownTypes.FileComponentBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                x.WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome(Environment.NewLine)
            })"
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}