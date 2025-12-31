using Discord.CX;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public abstract class BaseRendererTest(ITestOutputHelper output) : BaseTestWithDiagnostics(output)
{
    protected Compilation Compilation => _compilation ??= Compilations.Create();

    private Compilation? _compilation;

    protected enum ParseMode
    {
        AttributeValue,
        ElementValue
    }


    protected void AssertRenders(
        string? cx,
        CXValueGeneratorDelegate renderer,
        string? expected,
        ParseMode mode = ParseMode.AttributeValue,
        DesignerInterpolationInfo[]? interpolations = null,
        bool isAttributeValue = false,
        bool requiresValue = true,
        bool isOptional = false,
        string? propertyName = null,
        int? wrappingQuoteCount = null,
        CXValueGeneratorOptions? options = null
    )
    {
        AssertEmptyDiagnostics();

        ClearDiagnostics();

        CXValue? value = null;
        CXParser? parser = null;

        if (cx is not null)
        {
            var source = CXSourceText.From(cx);

            var interpolationSpans = interpolations?.Select(x => x.Span).ToArray();

            parser = new CXParser(source.CreateReader(
                wrappingQuoteCount: wrappingQuoteCount,
                interpolations: interpolationSpans
            ));

            switch (mode)
            {
                case ParseMode.AttributeValue:
                    value = parser.ParseAttributeValue();
                    break;
                case ParseMode.ElementValue:
                    if (!parser.TryParseElementChild([], out var child))
                        Assert.Fail("Can't parse as element value");

                    Assert.IsAssignableFrom<CXValue>(child);

                    value = (CXValue)child;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var document = new CXDocument(
                parser,
                [value]
            );
        }

        var context = new MockComponentContext(
            Compilation,
            new CXDesignerGeneratorState(
                cx ?? string.Empty,
                default,
                wrappingQuoteCount ?? 1,
                true,
                [..interpolations ?? []],
                null!,
                null!
            ),
            GeneratorOptions.Default
        );

        var propValue = new MockPropertyValue(
            value,
            null,
            value is not null,
            value is not null,
            isAttributeValue,
            requiresValue,
            isOptional,
            propertyName ?? "test-property",
            value?.Span ?? default
        );

        var result = renderer(context, new CXValueGeneratorTarget.ComponentProperty(propValue), options ?? CXValueGeneratorOptions.Default);

        PushDiagnostics(result.Diagnostics);

        if (expected is not null)
        {
            Assert.True(result.HasResult);
            Assert.Equal(expected, result.Value);
        }
    }

    private sealed record MockPropertyValue(
        CXValue? Value,
        GraphNode? GraphNode,
        bool IsSpecified,
        bool HasValue,
        bool IsAttributeValue,
        bool RequiresValue,
        bool IsOptional,
        string PropertyName,
        TextSpan Span
    ) : IComponentPropertyValue;
}