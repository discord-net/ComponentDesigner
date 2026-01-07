using Discord.CX.Parser;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public sealed class SyntaxDiagnosticTests(ITestOutputHelper output) : BaseParsingTest(output)
{
    [Fact]
    public void PartialElement()
    {
        Parses(
            "<",
            allowErrors: true
        );
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier(flags: CXTokenFlags.Missing);
                Token(CXTokenKind.ForwardSlashGreaterThan, flags: CXTokenFlags.Missing);
            }

            Diagnostic(
                CXErrorCode.MissingElementIdentifier,
                span: element.Span
            );
        }
        
        Parses(
            "<foo",
            allowErrors: true
        );
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                Token(CXTokenKind.ForwardSlashGreaterThan, flags: CXTokenFlags.Missing);
            }

            Diagnostic(
                CXErrorCode.UnexpectedToken,
                span: new(element.Span.End, 0)
            );
        }
    }
    
    [Fact]
    public void MissingElementClosingTag()
    {
        Parses(
            "<foo>bar",
            allowErrors: true
        );
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                Token(CXTokenKind.GreaterThan);

                Scalar();
                {
                    Token(CXTokenKind.Text, "bar");
                }

                Token(CXTokenKind.LessThanForwardSlash, flags: CXTokenFlags.Missing);
                Token(CXTokenKind.Identifier, flags: CXTokenFlags.Missing);
                Token(CXTokenKind.GreaterThan, flags: CXTokenFlags.Missing);
            }

            Diagnostic(
                CXErrorCode.MissingElementClosingTag,
                span: element.OpeningTag.Identifier!.Span
            );
        }
        
        Parses(
            "<foo>bar</",
            allowErrors: true
        );
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                Token(CXTokenKind.GreaterThan);

                Scalar();
                {
                    Token(CXTokenKind.Text, "bar");
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Token(CXTokenKind.Identifier, flags: CXTokenFlags.Missing);
                Token(CXTokenKind.GreaterThan, flags: CXTokenFlags.Missing);
            }

            Diagnostic(
                CXErrorCode.MissingElementClosingTag,
                span: element.OpeningTag.Identifier!.Span
            );
        }
        
        Parses(
            "<foo>bar</foo",
            allowErrors: true
        );
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                Token(CXTokenKind.GreaterThan);

                Scalar();
                {
                    Token(CXTokenKind.Text, "bar");
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Token(CXTokenKind.Identifier);
                Token(CXTokenKind.GreaterThan, flags: CXTokenFlags.Missing);
            }

            Diagnostic(
                CXErrorCode.MissingElementClosingTag,
                span: element.OpeningTag.Identifier!.Span
            );
        }
    }

    [Fact]
    public void InvalidRootElement()
    {
        Parses("</bar>", allowErrors: true);
        {
            Node<CXValue.Invalid>();
            
            Diagnostic(CXErrorCode.InvalidRootElement, span: new(0, 0));
        }
        
        Parses("<foo/></bar>", allowErrors: true);
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }
            
            Node<CXValue.Invalid>();
            
            Diagnostic(CXErrorCode.InvalidRootElement, span: new(element.FullSpan.End, 0));
        }
        
        Parses("<foo/>text</bar>", allowErrors: true);
        {
            var element = Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            var text = Node<CXValue.Scalar>();
            {
                Token(CXTokenKind.Text, "text");
            }
            
            Node<CXValue.Invalid>();
            
            Diagnostic(CXErrorCode.InvalidRootElement, span: new(text.FullSpan.End, 0));
        }
    }
    
    [Fact]
    public void AttributeMissingValue()
    {
        Parses(
            "<foo bar=/>",
            allowErrors: true
        );
        {
            CXAttribute attribute;
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");

                attribute = Attribute();
                {
                    Identifier("bar");
                    Token(CXTokenKind.Equals);

                    Node<CXValue.Invalid>();
                }
                
                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            Diagnostic(
                CXErrorCode.MissingAttributeValue,
                span: attribute.Span
            );
        }
    }
}