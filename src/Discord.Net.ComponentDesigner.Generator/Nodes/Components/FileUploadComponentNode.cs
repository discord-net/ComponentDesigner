using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class FileUploadComponentNode : ComponentNode
{
    public override string Name => "file-upload";

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public FileUploadComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            new(
                "customId",
                renderer: CXValueGenerator.String,
                validators: [Validators.StringRange(upper: Constants.CUSTOM_ID_MAX_LENGTH)]
            ),
            new(
                "min",
                isOptional: true,
                aliases: ["minValues"],
                renderer: CXValueGenerator.Integer,
                validators:
                [
                    Validators.IntRange(Constants.FILE_UPLOAD_MIN_VALUES_LOWER, Constants.FILE_UPLOAD_MIN_VALUES_UPPER)
                ]
            ),
            new(
                "max",
                isOptional: true,
                aliases: ["maxValues"],
                renderer: CXValueGenerator.Integer,
                validators:
                [
                    Validators.IntRange(Constants.FILE_UPLOAD_MAX_VALUES_LOWER, Constants.FILE_UPLOAD_MAX_VALUES_UPPER)
                ]
            ),
            new(
                "required",
                isOptional: true,
                renderer: CXValueGenerator.Boolean
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (context.KnownTypes.FileUploadComponentBuilderType is null)
        {
            diagnostics.Add(
                Diagnostics.MissingTypeInAssembly(nameof(context.KnownTypes.FileUploadComponentBuilderType)),
                state.Source
            );
        }

        // file uploads must be placed in labels, but we allow them as the root element.
        if (state.GraphNode?.Parent is not null && state.GraphNode.Parent.Inner is not LabelComponentNode)
        {
            diagnostics.Add(
                Diagnostics.FileUploadNotInLabel,
                state.Source
            );
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
            $"new {context.KnownTypes.FileUploadComponentBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                x.WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome(Environment.NewLine)
            })"
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}