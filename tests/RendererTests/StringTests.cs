using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class StringTests(ITestOutputHelper output) : BaseRendererTest(output)
{
    [Fact]
    public void BasicString()
    {
        AssertRenders(
            "\"Hello, World!\"",
            CXValueGenerator.String,
            "\"Hello, World!\""
        );
        
        AssertRenders(
            "\'Hello, World!\'",
            CXValueGenerator.String,
            "\"Hello, World!\""
        );
    }

    [Fact]
    public void MultilineString()
    {
        AssertRenders(
            """
            "Hello,
            World!"
            """,
            CXValueGenerator.String,
            // the empty line above the string literal is expected
            """"
            
            """
            Hello,
            World!
            """
            """"
        );

        AssertRenders(
            """
            "text

            with

            multiple



            breaks"
            """,
            CXValueGenerator.String,
            """"

            """
            text

            with

            multiple


            
            breaks
            """
            """"
        );
    }

    [Fact]
    public void StringWithInterpolations()
    {
        AssertRenders(
            """
            'Hello, {World}!'
            """,
            CXValueGenerator.String,
            """
            $"Hello, {designer.GetValueAsString(0)}!"
            """,
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new TextSpan(8, 7),
                    Compilation.GetSpecialType(SpecialType.System_String),
                    default
                )
            ]
        );

        var builder = new SourceBuilder()
            .AddSource("'Hello, ")
            .AddSource(Environment.NewLine)
            .AddInterpolation("World")
            .AddSource("!'");
        
        AssertRenders(
            builder.StringBuilder.ToString(),
            CXValueGenerator.String,
            """"
            
            $"""
             Hello, 
             {designer.GetValueAsString(0)}!
             """
            """",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    builder.Interpolations[0],
                    Compilation.GetSpecialType(SpecialType.System_String),
                    default
                )
            ]
        );
    }
}