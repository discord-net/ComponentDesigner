using Discord;
using Discord.CX;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class TimestampTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void ValueProperty()
    {
        Renders(
            """
            <time value="123" />
            """,
            "<t:123>"
        );
    }

    [Fact]
    public void SyntaxIndented()
    {
        Renders(
            """
            <time>
                123
            </time>
            """,
            "<t:123>"
        );
    }

    [Fact]
    public void Basic()
    {
        Renders(
            "<time>1618953630</time>",
            "<t:1618953630>"
        );
    }

    [Fact]
    public void InterpolatedDT()
    {
        var builder = new SourceBuilder()
            .AddSource("<time>")
            .AddInterpolation("DateTime.Now")
            .AddSource("</time>");

        Renders(
            builder.StringBuilder.ToString(),
            "<t:{designer.GetValue<System.DateTimeOffset>(0).ToUnixTimeSeconds()}>",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(
                        builder.Interpolations[0].Start,
                        builder.Interpolations[0].Length
                    ),
                    Compilation.GetTypeByMetadataName("System.DateTime"),
                    default
                )
            ]
        );
    }

    [Fact]
    public void InterpolatedDTO()
    {
        var builder = new SourceBuilder()
            .AddSource("<time>")
            .AddInterpolation("DateTimeOffset.UtcNow")
            .AddSource("</time>");

        Renders(
            builder.StringBuilder.ToString(),
            "<t:{designer.GetValue<System.DateTimeOffset>(0).ToUnixTimeSeconds()}>",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(
                        builder.Interpolations[0].Start,
                        builder.Interpolations[0].Length
                    ),
                    Compilation.GetTypeByMetadataName("System.DateTimeOffset"),
                    default
                )
            ]
        );
    }

    [Fact]
    public void InterpolatedNumber()
    {
        var builder = new SourceBuilder()
            .AddSource("<time>")
            .AddInterpolation("12345")
            .AddSource("</time>");

        Renders(
            builder.StringBuilder.ToString(),
            "<t:12345>",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(
                        builder.Interpolations[0].Start,
                        builder.Interpolations[0].Length
                    ),
                    Compilation.GetSpecialType(SpecialType.System_Int32),
                    new(12345)
                )
            ]
        );
    }

    [Fact]
    public void StyleShortHand()
    {
        AssertStyle("t");
        AssertStyle("T");
        AssertStyle("d");
        AssertStyle("D");
        AssertStyle("f");
        AssertStyle("F");
        AssertStyle("s");
        AssertStyle("S");
        AssertStyle("R");
    }

    [Fact]
    public void StyleFullName()
    {
        // as defined in Discord.TimestampTagStyles
        AssertStyle("ShortTime", "t");
        AssertStyle("LongTime", "T");
        AssertStyle("ShortDate", "d");
        AssertStyle("LongDate", "D");
        AssertStyle("ShortDateTime", "f");
        AssertStyle("LongDateTime", "F");
        AssertStyle("Relative", "R");
    }

    [Fact]
    public void InterpolatedStyleEnum()
    {
        var builder = new SourceBuilder()
            .AddSource("<time style=")
            .AddInterpolation("TimestampTagStyles.ShortDate")
            .AddSource(">12345</time>");

        Renders(
            builder.StringBuilder.ToString(),
            "<t:12345:{(char)designer.GetValue<global::Discord.TimestampTagStyles>(0)}>",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(
                        builder.Interpolations[0].Start,
                        builder.Interpolations[0].Length
                    ),
                    Compilation.GetTypeByMetadataName("Discord.TimestampTagStyles"),
                    default
                )
            ]
        );
    }

    private void AssertStyle(string style, string? expected = null)
    {
        expected ??= style;

        Renders(
            $"""
             <time style="{style}">123</time>
             """,
            $"<t:123:{expected}>"
        );
    }
}