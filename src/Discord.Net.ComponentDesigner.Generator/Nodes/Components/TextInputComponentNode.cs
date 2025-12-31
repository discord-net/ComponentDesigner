using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public TextInputComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            CustomId = new(
                "customId",
                isOptional: false,
                renderer: CXValueGenerator.String
            ),
            Style = new(
                "style",
                isOptional: true,
                renderer: CXValueGenerator.Enum(LIBRARY_TEXT_INPUT_STYLE_ENUM)
            ),
            MinLength = new(
                "minLength",
                aliases: ["min"],
                isOptional: true,
                renderer: CXValueGenerator.Integer,
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
                renderer: CXValueGenerator.Integer,
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
                renderer: CXValueGenerator.Boolean,
                requiresValue: false
            ),
            Value = new(
                "value",
                isOptional: true,
                renderer: CXValueGenerator.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.TEXT_INPUT_VALUE_MAX_LENGTH)
                ]
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                renderer: CXValueGenerator.String,
                validators:
                [
                    Validators.StringRange(upper: Constants.TEXT_INPUT_PLACEHOLDER_MAX_LENGTH)
                ]
            )
        ];
    }

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        base.Validate(state, context, diagnostics);
        Validators.Range(context, state, MinLength, MaxLength, diagnostics);
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"""
             new {context.KnownTypes.TextInputBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                     x.PrefixIfSome(4)
                         .WithNewlinePadding(4)
                         .WrapIfSome(Environment.NewLine)
                 })
             """
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}