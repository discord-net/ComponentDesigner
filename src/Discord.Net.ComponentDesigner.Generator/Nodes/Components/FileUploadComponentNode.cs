using System;
using System.Collections.Generic;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class FileUploadComponentNode : ComponentNode
{
    public override string Name => "file-upload";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public FileUploadComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            new(
                "customId",
                renderer: Renderers.String,
                validators: [Validators.StringRange(upper: Constants.CUSTOM_ID_MAX_LENGTH)]
            ),
            new(
                "min",
                isOptional: true,
                aliases: ["minValues"],
                renderer: Renderers.Integer,
                validators: [
                    Validators.IntRange(Constants.FILE_UPLOAD_MIN_VALUES_LOWER, Constants.FILE_UPLOAD_MIN_VALUES_UPPER)
                ]
            ),
            new(
                "max",
                isOptional: true,
                aliases: ["maxValues"],
                renderer: Renderers.Integer,
                validators: [
                    Validators.IntRange(Constants.FILE_UPLOAD_MAX_VALUES_LOWER, Constants.FILE_UPLOAD_MAX_VALUES_UPPER)
                ]
            ),
            new(
                "required",
                isOptional: true,
                renderer: Renderers.Boolean
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context)
    {
        if (context.KnownTypes.FileUploadComponentBuilderType is null)
        {
            context.AddDiagnostic(
                Diagnostics.MissingTypeInAssembly,
                state.Source,
                nameof(context.KnownTypes.FileUploadComponentBuilderType)
            );
        }
        
        // file uploads must be placed in labels, but we allow them as the root element.
        if (state.OwningNode?.Parent is not null && state.OwningNode.Parent.Inner is not LabelComponentNode)
        {
            context.AddDiagnostic(
                Diagnostics.FileUploadNotInLabel,
                state.Source
            );
        }
    }

    public override string Render(ComponentState state, IComponentContext context, ComponentRenderingOptions options)
        => $"""
            new {context.KnownTypes.FileUploadComponentBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                state.RenderProperties(this, context)
                    .WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome(Environment.NewLine)
            })
            """;
}