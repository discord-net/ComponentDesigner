using Discord.CX.Parser;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public sealed class IncrementalParsingTests(ITestOutputHelper output) : BaseIncrementalTests(output)
{
    [Fact]
    public void EntireSourceChanged()
    {
        ParseWithoutTreeAssert(
            """
            <foo />
            """
        );

        // by changing the trivia of the first token, and all remaining tokens, nothing should be incrementally reused
        Parse(
            """

            <bar></bar>
            """
        );
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: false);
                SimpleIdent("bar", reused: false);
                T(CXTokenKind.GreaterThan, reused: false);
                
                T(CXTokenKind.LessThanForwardSlash, reused: false);
                Ident("bar", reused: false);
                T(CXTokenKind.GreaterThan, reused: false);
            }
        }
    }
    
    [Fact]
    public void AttributeValueChanged()
    {
        ParseWithoutTreeAssert(
            """
            <foo bar="baz" />
            """
        );

        Parse(
            """
            <foo bar="baz2" />
            """
        );
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                SimpleIdent("foo", reused: true);

                Attribute(reused: false);
                {
                    Ident("bar", reused: true);
                    T(CXTokenKind.Equals, reused: true);

                    StrLiteral(reused: false);
                    {
                        T(CXTokenKind.StringLiteralStart, reused: true);
                        T(CXTokenKind.Text, value: "baz2", reused: false);
                        T(CXTokenKind.StringLiteralEnd, reused: true);
                    }
                }

                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
    
    [Fact]
    public void AdditionalAttributeAdded()
    {
        ParseWithoutTreeAssert(
            """
            <Foo bar baz />
            """
        );

        Parse(
            """
            <Foo bar add baz />
            """
        );
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                SimpleIdent("Foo", reused: true);

                // trivia changed
                Attribute(reused: false);
                {
                    Ident("bar", reused: false);
                }
                
                // added attribute
                Attribute(reused: false);
                {
                    Ident("add", reused: false);
                }
                
                // trivia remains the same
                Attribute(reused: true);
                {
                    Ident("baz", reused: true);
                }

                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
    
    [Fact]
    public void ElementChildChanges()
    {
        Parse(
            """
            <Parent>
                <Child1 />
                <Child2 />
            </Parent>
            """
        );
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                SimpleIdent("Parent");
                T(CXTokenKind.GreaterThan);

                Element();
                {
                    T(CXTokenKind.LessThan);
                    SimpleIdent("Child1");
                    T(CXTokenKind.ForwardSlashGreaterThan);
                }
                
                Element();
                {
                    T(CXTokenKind.LessThan);
                    SimpleIdent("Child2");
                    T(CXTokenKind.ForwardSlashGreaterThan);
                }
                
                T(CXTokenKind.LessThanForwardSlash);
                Ident("Parent");
                T(CXTokenKind.GreaterThan);
            }
        }
        
        Parse(
            """
            <Parent>
                <Child1 />
                <Child2 a/>
            </Parent>
            """
        );
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                SimpleIdent("Parent", reused: true);
                T(CXTokenKind.GreaterThan, reused: true);

                Element(reused: true);
                {
                    T(CXTokenKind.LessThan, reused: true);
                    SimpleIdent("Child1", reused: true);
                    T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
                }
                
                Element(reused: false);
                {
                    T(CXTokenKind.LessThan, reused: true);
                    SimpleIdent("Child2", reused: false);

                    Attribute(reused: false);
                    {
                        Ident("a", reused: false);
                    }
                    
                    T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
                }
                
                T(CXTokenKind.LessThanForwardSlash, reused: true);
                Ident("Parent", reused: true);
                T(CXTokenKind.GreaterThan, reused: true);
            }
        }
    }
    
    [Fact]
    public void AttributeAdded()
    {
        Parse("<foo />");
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                SimpleIdent("foo");
                T(CXTokenKind.ForwardSlashGreaterThan);
            }
        }

        Parse("<foo bar />");
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                SimpleIdent("foo", reused: false); // new trivia

                Attribute(reused: false);
                {
                    Ident("bar", reused: false);
                }

                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
    
    [Fact]
    public void AttributeRemoved()
    {
        Parse("<foo bar />");
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                SimpleIdent("foo");
                
                Attribute();
                {
                    Ident("bar");
                }
                
                T(CXTokenKind.ForwardSlashGreaterThan);
            }
        }

        Parse("<foo />");
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                SimpleIdent("foo", reused: false);
                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
    
    [Fact]
    public void AttributeNameChange()
    {
        Parse("<foo bar />");
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                SimpleIdent("foo");
                
                Attribute();
                {
                    Ident("bar");
                }
                
                T(CXTokenKind.ForwardSlashGreaterThan);
            }
        }

        Parse("<foo baz />");
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                SimpleIdent("foo", reused: true);
                
                Attribute(reused: false);
                {
                    Ident("baz", reused: false);
                }
                
                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
}