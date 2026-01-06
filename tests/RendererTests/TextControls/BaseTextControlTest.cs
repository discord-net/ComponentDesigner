using System.Diagnostics.CodeAnalysis;
using Discord.CX;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public abstract class BaseTextControlTest(ITestOutputHelper output) : BaseTestWithDiagnostics(output)
{
    protected Compilation Compilation { get; } = Compilations.Create();

    protected void Renders(
        [StringSyntax("html")] string cx,
        string? expected,
        DesignerInterpolationInfo[]? interpolations = null,
        int? wrappingQuoteCount = null,
        bool allowFail = false
    )
    {
        var parser = new CXParser(
            CXSourceText.From(cx).CreateReader(
                interpolations?.Select(x => new CXTextSpan( x.Span.Start,  x.Span.Length)).ToArray(),
                wrappingQuoteCount
            )
        );

        var parsed = parser.ParseElementChildren();

        PushDiagnostics(
            parsed.AllDiagnostics.Select(x => new DiagnosticInfo(
                Diagnostics.CreateParsingDiagnostic(x),
                x.Span
            ))
        );

        var diagnostics = new List<DiagnosticInfo>();

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

        if (
            !TextControlElement.TryCreate(
                context,
                parsed,
                diagnostics,
                out var element,
                out var nodesUsed
            )
        )
        {
            if (!allowFail) Assert.Fail("Failed to create text control elements");
        }

        if (element is not null)
        {
            Assert.Equal(parsed.Count, nodesUsed);

            element.Validate(context, diagnostics);

            PushDiagnostics(diagnostics);

            var result = element.Render(context);

            PushDiagnostics(result.Diagnostics);

            if (expected is not null)
            {
                Assert.True(result.HasResult);
                Assert.Equal(expected, result.Value);
            }
        }
        else
        {
            PushDiagnostics(diagnostics);
        }
    }
}