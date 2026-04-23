using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class Code(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Code Block";

        public override Result<TextControl> Render(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => GetFormattingOptions(context, element, options)
            .Combine(
                Join(RenderChildren(context, options, cancellationToken)),
                (formattingOptions, children) =>
                {
                    var isInline = formattingOptions.IsInline ?? !children.ContainsNewLines;
                    var childrenValue =
                    (
                        $"{children.LeadingTrivia.TrimLeadingSyntaxIndentation()}" +
                        $"{children.Value}" +
                        $"{children.TrailingTrivia.TrimTrailingSyntaxIndentation()}"
                    ).NormalizeIndentation();

                    var value = isInline
                        ? $"`{childrenValue}`"
                        : $"""
                           ```{formattingOptions.Language ?? string.Empty}
                           {childrenValue}
                           ```
                           """;

                    return new TextControl(
                        element,
                        value,
                        children.ValueContainsNewLines || !isInline
                    );
                }
            );

        private readonly record struct FormatOptions(
            bool? IsInline,
            string? Language
        );

        private static Result<FormatOptions> GetFormattingOptions(
            IRenderContext context,
            CXElement element,
            TextControlOptions options
        )
        {
            using var builder = Result<FormatOptions>.Builder;

            bool? isInline = null;
            string? language = null;

            foreach (var attribute in element.Attributes)
            {
                switch (attribute.Identifier)
                {
                    case "inline":
                    {
                        if (attribute.Value is null)
                        {
                            isInline = true;
                            continue;
                        }

                        if (!attribute.Value.TryGetLiteralValue(context, out var literal))
                        {
                            return Diagnostic
                                .ExpectedAConstantValue
                                .At(attribute.Value);
                        }

                        var value = literal.ToLowerInvariant();

                        if (value is "true" or "false")
                        {
                            isInline = value is "true";
                            continue;
                        }

                        builder.AddDiagnostic(
                            Diagnostic
                                .InvalidPropertyValue("inline", value)
                                .At(attribute.Value)
                        );
                        break;
                    }
                    case "lang" or "language":
                        if (attribute.Value is null)
                        {
                            builder.AddDiagnostic(
                                Diagnostic
                                    .RequiredPropertyValueNotSpecified(attribute.Identifier)
                                    .At(attribute)
                            );
                            
                            continue;
                        }

                        if (!TryGetTextBasedValue(context, attribute.Value, options, out language))
                            builder.AddDiagnostic(
                                Diagnostic
                                    .InvalidPropertyValue(
                                        attribute.Identifier,
                                        attribute.Value?.GetType().Name ?? "null"
                                    )
                                    .At(attribute.Value?.TextSpan ?? attribute.TextSpan)
                            );

                        break;
                }
            }

            return builder.WithValue(new(isInline, language)).Build();
        }
    }
}