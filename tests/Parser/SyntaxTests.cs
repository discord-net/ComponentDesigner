using ComponentDesigner;
using ComponentDesigner.Parser;
using Xunit.Abstractions;

namespace UnitTests.Parser;

public class SyntaxTests(ITestOutputHelper output) : BaseParserTest(output)
{
    [Fact]
    public void ElementInterpolatedIdentifier()
    {
        var source = new SourceBuilder()
            .AddSource("<")
            .AddInterpolation("interp")
            .AddSource(".Foo />");

        Parses(
            source.StringBuilder.ToString(),
            interpolations: source.Interpolations.ToArray()
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                Node<CXIdentifier.Interpolated>();
                {
                    Token(CXTokenKind.Interpolation, "{interp}");
                    Token(CXTokenKind.Qualifier);
                    Identifier("Foo");
                }
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }
        }
    }
    
    [Fact]
    public void HexEscapeSequence()
    {
        Parses(
            """
            <Foo>&#x3C0;</Foo>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXValue.Scalar>();
                {
                    Token(CXTokenKind.Text, value: "π");
                }
                
                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }
        }
    }
    
    [Fact]
    public void DecimalEscapeSequence()
    {
        Parses(
            """
            <Foo>&#960;</Foo>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXValue.Scalar>();
                {
                    Token(CXTokenKind.Text, value: "π");
                }
                
                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }
        }
    }
    
    [Fact]
    public void NamedEscapeSequences()
    {
        Parses(
            """
            <Foo>
                This &lt;Element&gt; is escaped
            </Foo>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXValue.Scalar>();
                {
                    Token(CXTokenKind.Text, value: "This <Element> is escaped");
                }
                
                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }
        }
    }
    
    [Fact]
    public void SingleCharElementValue()
    {
        Parses("<Foo>A</Foo>");
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXValue.Scalar>();
                {
                    Token(CXTokenKind.Text, "A");
                }
                
                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }
        }
    }
    
    [Fact]
    public void SingleElements()
    {
        Parses("<Foo />");
        {
            Node<CXElement>();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }
            EOF();
        }
    }

    [Fact]
    public void SingleElementWithEmptyChildren()
    {
        Parses(
            """
            <Foo>

            </Foo>
            """
        );
        {
            Node<CXElement>();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Token(CXTokenKind.LessThanForwardSlash);
                Token(CXTokenKind.Identifier, "Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void ElementWithChild()
    {
        Parses(
            """
            <Foo>
                <Bar/>
            </Foo>
            """
        );
        {
            Node<CXElement>();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXElement>();
                {
                    Token(CXTokenKind.LessThan);
                    SimpleIdentifier("Bar");
                    Token(CXTokenKind.ForwardSlashGreaterThan);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Token(CXTokenKind.Identifier, "Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void ElementsWithManyChildrenAndDepth()
    {
        Parses(
            """
            <Foo>
                <Bar/>
                <Baz>
                    <ABC>
                        <DEF/>
                    </ABC>
                    <GHI/>
                    <JKL>
                        <Depth1 />
                        <Depth2 />
                        <Depth3 />
                    </JKL>
                </Baz>
            </Foo>
            """
        );
        {
            Node<CXElement>();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXElement>();
                {
                    Token(CXTokenKind.LessThan);
                    SimpleIdentifier("Bar");
                    Token(CXTokenKind.ForwardSlashGreaterThan);
                }

                Node<CXElement>();
                {
                    Token(CXTokenKind.LessThan);
                    SimpleIdentifier("Baz");
                    Token(CXTokenKind.GreaterThan);

                    Node<CXElement>();
                    {
                        Token(CXTokenKind.LessThan);
                        SimpleIdentifier( "ABC");
                        Token(CXTokenKind.GreaterThan);

                        Node<CXElement>();
                        {
                            Token(CXTokenKind.LessThan);
                            SimpleIdentifier("DEF");
                            Token(CXTokenKind.ForwardSlashGreaterThan);
                        }

                        Token(CXTokenKind.LessThanForwardSlash);
                        Token(CXTokenKind.Identifier, "ABC");
                        Token(CXTokenKind.GreaterThan);
                    }

                    Node<CXElement>();
                    {
                        Token(CXTokenKind.LessThan);
                        SimpleIdentifier("GHI");
                        Token(CXTokenKind.ForwardSlashGreaterThan);
                    }

                    Node<CXElement>();
                    {
                        Token(CXTokenKind.LessThan);
                        SimpleIdentifier("JKL");
                        Token(CXTokenKind.GreaterThan);

                        Node<CXElement>();
                        {
                            Token(CXTokenKind.LessThan);
                            SimpleIdentifier("Depth1");
                            Token(CXTokenKind.ForwardSlashGreaterThan);
                        }

                        Node<CXElement>();
                        {
                            Token(CXTokenKind.LessThan);
                            SimpleIdentifier("Depth2");
                            Token(CXTokenKind.ForwardSlashGreaterThan);
                        }

                        Node<CXElement>();
                        {
                            Token(CXTokenKind.LessThan);
                            SimpleIdentifier("Depth3");
                            Token(CXTokenKind.ForwardSlashGreaterThan);
                        }

                        Token(CXTokenKind.LessThanForwardSlash);
                        Token(CXTokenKind.Identifier, "JKL");
                        Token(CXTokenKind.GreaterThan);
                    }


                    Token(CXTokenKind.LessThanForwardSlash);
                    Token(CXTokenKind.Identifier, "Baz");
                    Token(CXTokenKind.GreaterThan);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Token(CXTokenKind.Identifier, "Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void ElementWithSingleAttribute()
    {
        Parses(
            """
            <Foo bar="baz" />
            """
        );
        {
            Node<CXElement>();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Node<CXAttribute>();
                {
                    Token(CXTokenKind.Identifier, "bar");
                    Token(CXTokenKind.Equals);

                    Node<CXValue.StringLiteral>();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");
                        Token(CXTokenKind.Text, "baz");
                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void ElementWithManyAttributes()
    {
        Parses(
            """
            <Foo a1="abc" a2="" a3='hij'/>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("a1");
                    Token(CXTokenKind.Equals);

                    Node<CXValue.StringLiteral>();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");
                        Token(CXTokenKind.Text, "abc");
                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Attribute();
                {
                    Identifier("a2");
                    Token(CXTokenKind.Equals);

                    Node<CXValue.StringLiteral>();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");
                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Attribute();
                {
                    Identifier("a3");
                    Token(CXTokenKind.Equals);

                    Node<CXValue.StringLiteral>();
                    {
                        Token(CXTokenKind.StringLiteralStart, "'");
                        Token(CXTokenKind.Text, "hij");
                        Token(CXTokenKind.StringLiteralEnd, "'");
                    }
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void ElementWithValueAsChildren()
    {
        Parses(
            """
            <Foo>
                Some text here,
                Across multiple lines
            </Foo>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Node<CXValue.Scalar>();
                {
                    Token(
                        CXTokenKind.Text,
                        $"Some text here,{Environment.NewLine}    Across multiple lines"
                    );
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void InterpolationInAttribute()
    {
        Parses(
            """
            <Foo bar={This is interpolated} />
            """,
            interpolations: [CXTextSpan.FromBounds(9, 31)]
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("bar");
                    Token(CXTokenKind.Equals);

                    var interpolation = Node<CXValue.Interpolation>();
                    {
                        Assert.Equal(0, interpolation.InterpolationIndex);

                        Token(CXTokenKind.Interpolation, "{This is interpolated}");
                    }
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void StringLiteralAttributeWithInterpolatedPart()
    {
        Parses(
            """
            <Foo bar="abc{Interpolation}def" />
            """,
            interpolations: [CXTextSpan.FromBounds(13, 28)]
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("bar");
                    Token(CXTokenKind.Equals);

                    StringLiteral();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");

                        Token(CXTokenKind.Text, "abc");

                        var interpolation = Token(CXTokenKind.Interpolation, "{Interpolation}");
                        Assert.Equal(0, Document.GetInterpolationIndex(interpolation));

                        Token(CXTokenKind.Text, "def");

                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void StringLiteralAttributeWithManyInterpolatedParts()
    {
        Parses(
            new SourceBuilder()
                .AddSource("<Foo bar=\"abc")
                .AddInterpolation("Interpolated 1")
                .AddSource("def")
                .AddInterpolation("Interpolated 2")
                .AddSource(" some other text ")
                .AddInterpolation("Interpolated 3")
                .AddSource("\" />")
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("bar");
                    Token(CXTokenKind.Equals);

                    StringLiteral();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");

                        Token(CXTokenKind.Text, "abc");
                        InterpolationToken("{Interpolated 1}", 0);
                        Token(CXTokenKind.Text, "def");
                        InterpolationToken("{Interpolated 2}", 1);
                        Token(CXTokenKind.Text, " some other text ");
                        InterpolationToken("{Interpolated 3}", 2);

                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void MultipartElementChildren()
    {
        Parses(
            new SourceBuilder()
                .AddSource("<Foo>Some body text ")
                .AddInterpolation("Interpolated 1")
                .AddSource(" And some other text")
                .AddInterpolation("Interpolated 2")
                .AddSource("</Foo>")
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Multipart();
                {
                    Token(CXTokenKind.Text, "Some body text");
                    InterpolationToken("{Interpolated 1}", 0);
                    Token(CXTokenKind.Text, "And some other text");
                    InterpolationToken("{Interpolated 2}", 1);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void XMLComments()
    {
        Parses(
            """
            <!-- This comment is at the start -->
            <Foo>
                <!-- Comment in the body <Bar/> -->
                <Bar />
                <!-- Another comment here -->
            </Foo>
            <!-- Comment at the end --> 
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Element();
                {
                    Token(CXTokenKind.LessThan);
                    SimpleIdentifier("Bar");
                    Token(CXTokenKind.ForwardSlashGreaterThan);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void Fragments()
    {
        Parses(
            """
            <Foo>
                <>
                    <Bar />
                    <Baz />
                </>
            </Foo>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                var fragment = Element();
                {
                    Assert.Null(fragment.OpeningTag.Identifier);
                    Assert.True(fragment.IsFragment);

                    Token(CXTokenKind.LessThan);
                    Token(CXTokenKind.GreaterThan);

                    Element();
                    {
                        Token(CXTokenKind.LessThan);
                        SimpleIdentifier("Bar");
                        Token(CXTokenKind.ForwardSlashGreaterThan);
                    }

                    Element();
                    {
                        Token(CXTokenKind.LessThan);
                        SimpleIdentifier("Baz");
                        Token(CXTokenKind.ForwardSlashGreaterThan);
                    }

                    Token(CXTokenKind.LessThanForwardSlash);
                    Token(CXTokenKind.GreaterThan);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void FragmentWithAttributes()
    {
        Parses(
            """
            <foo="bar" baz="abc">

            </>
            """
        );
        {
            var frag = Element();
            {
                Assert.True(frag.IsFragment);

                Token(CXTokenKind.LessThan);

                Attribute();
                {
                    Identifier("foo");
                    Token(CXTokenKind.Equals);
                    StringLiteral();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");
                        Token(CXTokenKind.Text, "bar");
                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Attribute();
                {
                    Identifier("baz");
                    Token(CXTokenKind.Equals);
                    StringLiteral();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");
                        Token(CXTokenKind.Text, "abc");
                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }

                Token(CXTokenKind.GreaterThan);

                Token(CXTokenKind.LessThanForwardSlash);
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void MultipleRootElements()
    {
        Parses(
            """
            <Foo />
            <Bar>
                <Baz/>
            </Bar>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Bar");
                Token(CXTokenKind.GreaterThan);

                Element();
                {
                    Token(CXTokenKind.LessThan);
                    SimpleIdentifier("Baz");
                    Token(CXTokenKind.ForwardSlashGreaterThan);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Bar");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void RootElementAndValue()
    {
        Parses(
            new SourceBuilder()
                .AddSource(
                    """
                    <Foo>
                        <Bar/>
                    </Foo>


                    """
                )
                .AddInterpolation("Interp")
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");
                Token(CXTokenKind.GreaterThan);

                Element();
                {
                    Token(CXTokenKind.LessThan);
                    SimpleIdentifier("Bar");
                    Token(CXTokenKind.ForwardSlashGreaterThan);
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("Foo");
                Token(CXTokenKind.GreaterThan);
            }

            Interpolation();
            {
                InterpolationToken("{Interp}", 0);
            }
            
            EOF();
        }
    }

    [Fact]
    public void AttributeWithElementValue()
    {
        Parses(
            """
            <Foo
                bar=(<Baz />)
            />
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("bar");
                    Token(CXTokenKind.Equals);

                    ElementValue();
                    {
                        Token(CXTokenKind.OpenParenthesis);
                        Element();
                        {
                            Token(CXTokenKind.LessThan);
                            SimpleIdentifier("Baz");
                            Token(CXTokenKind.ForwardSlashGreaterThan);
                        }
                        Token(CXTokenKind.CloseParenthesis);
                    }
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }
            
            EOF();
        }
    }

    [Fact]
    public void AttributesWithoutValues()
    {
        Parses(
            """
            <Foo bar baz />
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("bar");
                }
                
                Attribute();
                {
                    Identifier("baz");
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }
            
            EOF();
        }
    }
    
    [Fact]
    public void AttributesWithAndWithoutValues()
    {
        Parses(
            """
            <Foo abc="def" bar baz='123' test />
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("Foo");

                Attribute();
                {
                    Identifier("abc");
                    Token(CXTokenKind.Equals);

                    StringLiteral();
                    {
                        Token(CXTokenKind.StringLiteralStart, "\"");
                        Token(CXTokenKind.Text, "def");
                        Token(CXTokenKind.StringLiteralEnd, "\"");
                    }
                }
                
                Attribute();
                {
                    Identifier("bar");
                }

                Attribute();
                {
                    Identifier("baz");
                    Token(CXTokenKind.Equals);

                    StringLiteral();
                    {
                        Token(CXTokenKind.StringLiteralStart, "'");
                        Token(CXTokenKind.Text, "123");
                        Token(CXTokenKind.StringLiteralEnd, "'");
                    }
                }
                
                Attribute();
                {
                    Identifier("test");
                }
                
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }
            
            EOF();
        }
    }
}