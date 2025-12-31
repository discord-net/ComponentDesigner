using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class BooleanTests(ITestOutputHelper output) : BaseRendererTest(output)
{
    [Fact]
    public void BasicBoolean()
    {
        AssertRenders(
            "'true'",
            CXValueGenerator.Boolean,
            "true"
        );
        
        AssertRenders(
            "'false'",
            CXValueGenerator.Boolean,
            "false"
        );
    }

    [Fact]
    public void BooleanWithOddCasing()
    {
        AssertRenders(
            "'tRUe'",
            CXValueGenerator.Boolean,
            "true"
        );
        
        AssertRenders(
            "'FALSE'",
            CXValueGenerator.Boolean,
            "false"
        );
    }

    [Fact]
    public void UnspecifiedBooleanProperty()
    {
        AssertRenders(
            cx: null,
            CXValueGenerator.Boolean,
            "true",
            requiresValue: false
        );
    }

    [Fact]
    public void InvalidBooleanValue()
    {
        AssertRenders(
            "'blah'",
            CXValueGenerator.Boolean,
            null
        );
        {
            Diagnostic(Diagnostics.TypeMismatch(expected: "bool", actual: "string"));
            
            EOF();
        }
    }

    [Fact]
    public void InterpolatedConstant()
    {
        AssertRenders(
            "'{Interp}'",
            CXValueGenerator.Boolean,
            "false",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(1, 8),
                    Compilation.GetSpecialType(SpecialType.System_Boolean),
                    new(false)
                )
            ]
        );
    }

   
}