using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class IntegerTests(ITestOutputHelper output) : BaseRendererTest(output)
{
    [Fact]
    public void BasicIntegers()
    {
        AssertRenders(
            "'123'",
            CXValueGenerator.Integer,
            "123"
        );

        AssertRenders(
            "'-456'",
            CXValueGenerator.Integer,
            "-456"
        );
    }

    [Fact]
    public void RuntimeFallback()
    {
        AssertRenders(
            $"'{uint.MaxValue}'",
            CXValueGenerator.Integer,
            $"int.Parse(\"{uint.MaxValue}\")"
        );
        {
            Diagnostic(Diagnostics.FallbackToRuntimeValueParsing("int.Parse"));

            EOF();
        }
    }

    [Fact]
    public void InterpolatedConstant()
    {
        AssertRenders(
            "'{Interp}'",
            CXValueGenerator.Integer,
            "123",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(1, 8),
                    Compilation.GetSpecialType(SpecialType.System_Int32),
                    new(123)
                )
            ]
        );
    }
}