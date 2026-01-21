using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class ThumbnailComponentNode : ComponentNode
{
    public override string Name => "thumbnail";

    public ComponentProperty Id { get; }
    public ComponentProperty Media { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Spoiler { get; }

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public ThumbnailComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Media = new(
                "media",
                aliases: ["href", "url"],
                renderer: CXValueGenerator.UnfurledMediaItem
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: CXValueGenerator.String,
                validators: [Validators.StringRange(upper: Constants.THUMBNAIL_DESCRIPTION_MAX_LENGTH)]
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                renderer: CXValueGenerator.Boolean,
                dotnetParameterName: "isSpoiler",
                requiresValue: false
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
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"""
             new {context.KnownTypes.ThumbnailBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                 x.PrefixIfSome(4)
                     .WithNewlinePadding(4)
                     .WrapIfSome(Environment.NewLine)
             })
             """
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}