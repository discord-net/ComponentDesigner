using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class TextDisplayTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyTextDisplay()
    {
        Graph("<text />");
        {
            var text = Component<TextDisplayComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.RequiredPropertyNotSpecified(text, text.Content)
                );
            }
        }
    }

    [Fact]
    public void LiteralAttributeContent()
    {
        Graph("<text content='hello' />");
        {
            Component<TextDisplayComponentNode>();

            Emits(
                """
                new global::Discord.TextDisplayBuilder(
                    content: "hello"
                )
                """
            );
        }
    }

    [Fact]
    public void MultipartAttributeContent()
    {
        Graph(
            """
            <text content='hello {world}' />
            """,
            pretext:
            "var world = \"foo\";"
        );
        {
            Component<TextDisplayComponentNode>();

            Emits(
                """
                new global::Discord.TextDisplayBuilder(
                    content: $"hello {designer.GetValueAsString(0)}"
                )
                """
            );
        }
    }

    [Fact]
    public void ContentAsChildren()
    {
        Graph(
            """
            <text>
                Hello, world!
            </text>
            """
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """
                new global::Discord.TextDisplayBuilder(
                    content: "Hello, world!"
                )
                """
            );
        }
    }

    [Fact]
    public void MultilineContentAsChildren()
    {
        Graph(
            """
            <text>
                This content contains multiple lines:
                  - The indentation is preserved based on the shortest line
                So we can do 
                  multi
                    line
                      indentation
                
                
                and multiple breaks
            </text>
            """
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """"
                new global::Discord.TextDisplayBuilder(
                    content: 
                    """
                    This content contains multiple lines:
                      - The indentation is preserved based on the shortest line
                    So we can do 
                      multi
                        line
                          indentation
                    
                    
                    and multiple breaks
                    """
                )
                """"
            );
        }
    }

    [Fact]
    public void QuoteOverflow()
    {
        Graph(
            """
            <text>Foo ""</text>
            """
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """"
                new global::Discord.TextDisplayBuilder(
                    content: 
                    """
                    Foo ""
                    """
                )
                """"
            );
        }
    }

    [Fact]
    public void MultilineQuoteOverflow()
    {
        Graph(
            """""
            <text>
                """Hello""" 
                """"World""""
            </text>
            """"",
            quoteCount: 5
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """""""
                new global::Discord.TextDisplayBuilder(
                    content: 
                    """""
                    """Hello""" 
                    """"World""""
                    """""
                )
                """""""
            );
        }
    }

    [Fact]
    public void WithTextControls()
    {
        Graph(
            """
            <text>
                <b>Hello</b>, <u>World!</u>
            </text>
            """
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """
                new global::Discord.TextDisplayBuilder(
                    content: "**Hello**, __World!__"
                )
                """
            );
        }
    }

    [Fact]
    public void MultiLineInterpolation()
    {
        Graph(
            """
            <text>
                {a}
                {b}
            </text>
            """,
            pretext:
            """
            string a = Random.Shared.Next().ToString();
            string b = Random.Shared.Next().ToString();
            """
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """"
                new global::Discord.TextDisplayBuilder(
                    content: 
                    $"""
                     {designer.GetValueAsString(0)}
                     {designer.GetValueAsString(1)}
                     """
                )
                """"
            );
            
        }
    }
}