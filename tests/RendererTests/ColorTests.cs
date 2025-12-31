using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class ColorTests(ITestOutputHelper output) : BaseRendererTest(output)
{
    [Fact]
    public void PredefinedColors()
    {
        AssertRenders(
            "'red'",
            CXValueGenerator.Color,
            "global::Discord.Color.Red"
        );
        
        AssertRenders(
            "'blue'",
            CXValueGenerator.Color,
            "global::Discord.Color.Blue"
        );
    }

    [Fact]
    public void NotAColorRuntimeFallback()
    {
        AssertRenders(
            "'blah'",
            CXValueGenerator.Color,
            "global::Discord.Color.Parse(\"blah\")"
        );
        {
            Diagnostic(Diagnostics.FallbackToRuntimeValueParsing("Discord.Color.Parse"));
            EOF();
        }
    }

    [Fact]
    public void Hex()
    {
        AssertRenders(
            "'00FF00'",
            CXValueGenerator.Color,
            "new global::Discord.Color(65280)"
        );
        
        AssertRenders(
            "'#00FF00'",
            CXValueGenerator.Color,
            "new global::Discord.Color(65280)"
        );
    }

    [Fact]
    public void InterpolatedConstantHex()
    {
        AssertRenders(
            "'{Interp}'",
            CXValueGenerator.Color,
            "new global::Discord.Color(65280)",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(1, 8),
                    Compilation.GetSpecialType(SpecialType.System_UInt32),
                    new(0x00FF00)
                )
            ]
        );
    }
}