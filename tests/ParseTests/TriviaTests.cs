using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public sealed class TriviaTests(ITestOutputHelper output) : BaseParsingTest(output)
{
    [Fact]
    public void BasicWhitespace()
    {
        Parses(
            """
            <foo />
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                var ident = SimpleIdentifier("foo");

                TrailingTrivia(ident);
                {
                    TriviaToken(CXTriviaTokenKind.Whitespace, value: " ");
                }

                Token(CXTokenKind.ForwardSlashGreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void MultiLineTrivia()
    {
        Parses(
            """
            <foo>
                <bar />
            </foo>
            """
        );
        {
            Element();
            {
                Token(CXTokenKind.LessThan);
                SimpleIdentifier("foo");
                TrailingTrivia(Token(CXTokenKind.GreaterThan));
                {
                    Newline(value: Environment.NewLine);
                }

                Element();
                {
                    LeadingTrivia(Token(CXTokenKind.LessThan));
                    {
                        Whitespace(length: 4);
                    }

                    TrailingTrivia(SimpleIdentifier("bar"));
                    {
                        Whitespace(length: 1);
                    }

                    TrailingTrivia(Token(CXTokenKind.ForwardSlashGreaterThan));
                    {
                        Newline(value: Environment.NewLine);
                    }
                }

                Token(CXTokenKind.LessThanForwardSlash);
                Identifier("foo");
                Token(CXTokenKind.GreaterThan);
            }

            EOF();
        }
    }

    [Fact]
    public void XmlComments()
    {
        Parses(
            """
            <!-- Comment 1 -->
            <foo>
                <!-- comment 2-->
                <bar/>
                <!--comment 3-->
            </foo>
            <!-- Comment 4: not actually parsed -->
            """
        );
        {
            Element();
            {
                LeadingTrivia(Token(CXTokenKind.LessThan));
                {
                    XmlComment();
                    {
                        CommentStart();
                        Comment(value: " Comment 1 ");
                        CommentEnd();
                    }

                    Newline(value: Environment.NewLine);
                }

                SimpleIdentifier("foo");
                TrailingTrivia(Token(CXTokenKind.GreaterThan));
                {
                    Newline(value: Environment.NewLine);
                }

                Element();
                {
                    LeadingTrivia(Token(CXTokenKind.LessThan));
                    {
                        Whitespace(length: 4);

                        XmlComment();
                        {
                            CommentStart();
                            Comment(value: " comment 2");
                            CommentEnd();
                        }

                        Newline(value: Environment.NewLine);
                        Whitespace(length: 4);
                    }
                    SimpleIdentifier("bar");
                    TrailingTrivia(Token(CXTokenKind.ForwardSlashGreaterThan));
                    {
                        Newline(value: Environment.NewLine);
                    }
                }

                LeadingTrivia(Token(CXTokenKind.LessThanForwardSlash));
                {
                    Whitespace(length: 4);

                    XmlComment();
                    {
                        CommentStart();
                        Comment(value: "comment 3");
                        CommentEnd();
                    }

                    Newline(value: Environment.NewLine);
                }
                Identifier("foo");
                TrailingTrivia(Token(CXTokenKind.GreaterThan));
                {
                    Newline(value: Environment.NewLine);
                }
            }
            
            EOF();
        }
    }

    #region Scaffold

    private readonly Queue<CXTrivia> _trivia = [];

    private void Trivia(ICXNode node)
    {
        LeadingTrivia(node);
        TrailingTrivia(node);
    }

    private void LeadingTrivia(ICXNode node)
    {
        Assert.Empty(_trivia);

        foreach (var trivia in node.LeadingTrivia.SelectMany(Enumerate))
        {
            _trivia.Enqueue(trivia);
        }
    }

    private void TrailingTrivia(ICXNode node)
    {
        Assert.Empty(_trivia);

        foreach (var trivia in node.TrailingTrivia.SelectMany(Enumerate))
        {
            _trivia.Enqueue(trivia);
        }
    }

    private IEnumerable<CXTrivia> Enumerate(CXTrivia trivia)
        => trivia switch
        {
            CXTrivia.XmlComment(var start, var val, var end) =>
            [
                trivia,
                start,
                val,
                ..(end is null ? (IEnumerable<CXTrivia>)[] : [end])
            ],
            _ => [trivia]
        };

    private CXTrivia.XmlComment XmlComment()
    {
        Assert.NotEmpty(_trivia);

        var trivia = _trivia.Dequeue();

        Assert.IsType<CXTrivia.XmlComment>(trivia);
        
        return (CXTrivia.XmlComment)trivia;
    }

    private CXTrivia.Token CommentStart(
        string? value = null,
        int? length = null
    ) => TriviaToken(CXTriviaTokenKind.CommentStart, value, length);

    private CXTrivia.Token CommentEnd(
        string? value = null,
        int? length = null
    ) => TriviaToken(CXTriviaTokenKind.CommentEnd, value, length);

    private CXTrivia.Token Comment(
        string? value = null,
        int? length = null
    ) => TriviaToken(CXTriviaTokenKind.Comment, value, length);

    private CXTrivia.Token Newline(
        string? value = null,
        int? length = null
    ) => TriviaToken(CXTriviaTokenKind.Newline, value, length);

    private CXTrivia.Token Whitespace(
        string? value = null,
        int? length = null
    ) => TriviaToken(CXTriviaTokenKind.Whitespace, value, length);

    private CXTrivia.Token TriviaToken(
        CXTriviaTokenKind kind,
        string? value = null,
        int? length = null
    )
    {
        Assert.NotEmpty(_trivia);

        var trivia = _trivia.Dequeue();

        Assert.IsType<CXTrivia.Token>(trivia);

        var triviaToken = (CXTrivia.Token)trivia;

        Assert.Equal(kind, triviaToken.Kind);

        if (value is not null)
            Assert.Equal(value, triviaToken.Value);

        if (length is not null)
            Assert.Equal(length.Value, triviaToken.Length);

        return triviaToken;
    }

    protected override void EOF()
    {
        Assert.Empty(_trivia);
        base.EOF();
    }

    #endregion
}