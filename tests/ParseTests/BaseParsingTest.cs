using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord.CX.Parser;
using Discord.CX.Parser.DebugUtils;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public abstract class BaseParsingTest(ITestOutputHelper output) : IDisposable
{
    public static readonly TimeSpan TestTimeout =
#if DEBUG
        TimeSpan.FromHours(1);
#else
        TimeSpan.FromSeconds(10);
#endif

    protected CXDocument? Document { get; private set; }
    private IEnumerator<ICXNode>? _enumerator;
    private readonly Stack<CXDiagnostic> _diagnostics = [];


    protected CXDiagnostic Diagnostic(CXErrorCode code, string? message = null, TextSpan? span = null)
    {
        Assert.NotEmpty(_diagnostics);
        var diagnostic = _diagnostics.Pop();

        Assert.Equal(code, diagnostic.Code);

        if (message is not null) Assert.Equal(message, diagnostic.Message);

        if (span.HasValue) Assert.Equal(span.Value, diagnostic.Span);

        return diagnostic;
    }

    [MemberNotNull(nameof(Document))]
    protected void Parses(
        SourceBuilder source,
        Func<CXParser, IEnumerable<CXNode>>? parseFunc = null,
        bool allowErrors = false
    ) => Parses(source.StringBuilder.ToString(), parseFunc, source.Interpolations.ToArray(), allowErrors);

    [MemberNotNull(nameof(Document))]
    protected void Parses(
        [StringSyntax("html")]string cx,
        Func<CXParser, IEnumerable<CXNode>>? parseFunc = null,
        TextSpan[]? interpolations = null,
        bool allowErrors = false
    )
    {
        parseFunc ??= (parser) => parser.ParseTopLevelNodes();

        output.WriteLine($"Parsing:\n{cx}");

        var token = new CancellationTokenSource(TestTimeout).Token;

        var parser = new CXParser(
            CXSourceText.From(cx).CreateReader(interpolations: interpolations),
            token
        );

        var nodes = parseFunc(parser).ToList();

        Document = new CXDocument(parser, nodes);

        var structural = Document.ToStructuralFormat();
        var dot = Document.ToDOTFormat();

        output.WriteLine($"AST:\n{structural}");
        output.WriteLine($"DOT:\n{dot}");

        if (!allowErrors) Assert.False(Document.HasErrors);

        foreach (var diagnostic in Document.AllDiagnostics)
        {
            _diagnostics.Push(diagnostic);
        }

        _enumerator = nodes
            .SelectMany(Enumerate)
            .GetEnumerator();
    }

    private IEnumerable<ICXNode> Enumerate(ICXNode node)
    {
        yield return node;

        if (node.Slots.Count is 0) yield break;

        foreach (var slot in node.Slots)
        foreach (var next in ProcessNode(slot))
        {
            yield return next;
        }

        IEnumerable<ICXNode> ProcessNode(ICXNode node)
        {
            if (node is ICXCollection collection)
            {
                // don't return the collection type
                return collection.ToList().SelectMany(ProcessNode);
            }

            return Enumerate(node);
        }
    }

    private void Reset()
    {
        _enumerator = null;
    }

    private ICXNode GetNextNode()
    {
        Assert.NotNull(_enumerator);
        Assert.True(_enumerator.MoveNext());

        return _enumerator.Current;
    }

    protected CXElement Element(string? content = null) => Node<CXElement>(content);
    protected CXAttribute Attribute(string? content = null) => Node<CXAttribute>(content);

    protected CXValue.Element ElementValue(string? content = null) => Node<CXValue.Element>(content);
    protected CXValue.StringLiteral StringLiteral(string? content = null) => Node<CXValue.StringLiteral>(content);

    protected CXValue.Interpolation Interpolation(string? content = null, int? index = null)
    {
        var interpolation = Node<CXValue.Interpolation>(content);

        if (index is not null) Assert.Equal(index.Value, interpolation.InterpolationIndex);

        return interpolation;
    }

    protected CXValue.Scalar Scalar(string? content = null) => Node<CXValue.Scalar>(content);

    protected CXValue.Multipart Multipart(string? content = null) => Node<CXValue.Multipart>(content);

    protected T Node<T>(string? content = null) where T : ICXNode
    {
        var current = GetNextNode();

        Assert.IsType<T>(current);

        if (content is not null) Assert.Equal(content, current.ToString());

        return (T)current;
    }

    public CXIdentifier SimpleIdentifier(string? content = null, CXTokenFlags? flags = null)
    {
        var node = Node<CXIdentifier.Simple>();
        {
            Identifier(content, flags);
        }

        return node;
    }

    protected CXToken Identifier(string content, CXTokenFlags? flags = null)
        => Token(CXTokenKind.Identifier, content, flags: flags);

    protected CXToken InterpolationToken(string? content = null, int? index = null)
    {
        Assert.NotNull(Document);

        var token = Token(CXTokenKind.Interpolation, content);

        if (index.HasValue) Assert.Equal(index.Value, Document.GetInterpolationIndex(token));

        return token;
    }

    protected CXToken Token(CXTokenKind kind, string? content = null, string? value = null, CXTokenFlags? flags = null)
    {
        var current = GetNextNode();

        Assert.IsType<CXToken>(current);

        var token = (CXToken)current;

        Assert.Equal(kind, token.Kind);

        if (content is not null)
            Assert.Equal(content, token.RawValue);
        
        if(value is not null)
            Assert.Equal(value, token.Value);

        if (flags is not null) Assert.Equal(flags.Value, token.Flags);

        return token;
    }

    protected virtual void EOF()
    {
        Assert.NotNull(_enumerator);
        Assert.False(_enumerator.MoveNext());
        Assert.Empty(_diagnostics);
    }

    public void Dispose()
    {
        EOF();
    }
}