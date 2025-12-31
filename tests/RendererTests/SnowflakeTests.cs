using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class SnowflakeTests(ITestOutputHelper output) : BaseRendererTest(output)
{
    [Fact]
    public void BasicSnowflake()
    {
        AssertRenders(
            "'123'",
            CXValueGenerator.Snowflake,
            "123"
        );
        
        AssertRenders(
            $"'{ulong.MaxValue}'",
            CXValueGenerator.Snowflake,
            $"{ulong.MaxValue}"
        );
    }

    [Fact]
    public void SnowflakeOutOfRangeUsingFallback()
    {
        AssertRenders(
            "'-1'",
            CXValueGenerator.Snowflake,
            "ulong.Parse(\"-1\")"
        );
        {
            Diagnostic(Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"));
            EOF();
        }
        
        AssertRenders(
            $"'18446744073709551616'",
            CXValueGenerator.Snowflake,
            "ulong.Parse(\"18446744073709551616\")"
        );
        {
            Diagnostic(Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"));
            EOF();
        }
    }

    [Fact]
    public void InterpolatedConstant()
    {
        AssertRenders(
            "'{Interp}'",
            CXValueGenerator.Snowflake,
            "123",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(1, 8),
                    Compilation.GetSpecialType(SpecialType.System_UInt64),
                    new(123ul)
                )
            ]
        );
    }
}