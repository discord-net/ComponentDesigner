using System;
using System.Collections.Generic;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class TextInputComponentNode : ComponentNode
{
    public const string LIBRARY_TEXT_INPUT_STYLE_ENUM = "Discord.TextInputStyle";

    public override string Name => "text-input";

    public override IReadOnlyList<string> Aliases { get; } = ["input"];

    public ComponentProperty Id { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty Style { get; }
    public ComponentProperty MinLength { get; }
    public ComponentProperty MaxLength { get; }
    public ComponentProperty Required { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Placeholder { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public TextInputComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            CustomId = new(
                "customId",
                isOptional: false,
                renderer: Renderers.String
            ),
            Style = new(
                "style",
                isOptional: true,
                renderer: Renderers.RenderEnum(LIBRARY_TEXT_INPUT_STYLE_ENUM)
            ),
            MinLength = new(
                "minLength",
                aliases: ["min"],
                isOptional: true,
                renderer: Renderers.Integer,
                validators:
                [
                    Validators.IntRange(
                        Constants.TEXT_INPUT_MIN_LENGTH_MIN_VALUE,
                        Constants.TEXT_INPUT_MIN_LENGTH_MAX_VALUE
                    )
                ]
            ),
            MaxLength = new(
                "maxLength",
                aliases: ["max"],
                isOptional: true,
                renderer: Renderers.Integer,
                validators:
                [
                    Validators.IntRange(
                        Constants.TEXT_INPUT_MAX_LENGTH_MIN_VALUE,
                        Constants.TEXT_INPUT_MAX_LENGTH_MAX_VALUE
                    )
                ]
            ),
            Required = new(
                "required",
                isOptional: true,
                renderer: Renderers.Boolean,
                requiresValue: false
            ),
            Value = new(
                "value",
                isOptional: true,
                renderer: Renderers.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.TEXT_INPUT_VALUE_MAX_LENGTH)
                ]
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                renderer: Renderers.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.TEXT_INPUT_PLACEHOLDER_MAX_LENGTH)
                ]
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context)
    {
        base.Validate(state, context);

        Validators.Range(context, state, MinLength, MaxLength);
    }

    public override string Render(ComponentState state, IComponentContext context, ComponentRenderingOptions options)
        => $"""
            new {context.KnownTypes.TextInputBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                state.RenderProperties(this, context)
                    .PrefixIfSome(4)
                    .WithNewlinePadding(4)
                    .WrapIfSome(Environment.NewLine)
            })
            """;
}