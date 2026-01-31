using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord.CX;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;
using UnitTests.Utils;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public abstract class BaseIncrementalTests(ITestOutputHelper output) : IDisposable
{
    private CXDocument? _document;
    private IEnumerator<ICXNode>? _enumerator;
    private readonly Stack<CXDiagnostic> _diagnostics = [];

    protected CXDocument ParseWithoutTreeAssert(
        [StringSyntax("html")] string cx,
        [StringSyntax("csharp")] string? pretext = null,
        bool allowParsingErrors = false,
        GeneratorOptions? options = null,
        [StringSyntax("csharp")] string? additionalMethods = null,
        string testClassName = "TestClass",
        string testFuncName = "Run",
        bool hasInterpolations = true,
        int quoteCount = 3
    )
    {
        var doc = Parse(
            cx, pretext, allowParsingErrors, options, additionalMethods, testClassName, testFuncName,
            hasInterpolations, quoteCount
        );

        _enumerator = null;

        return doc;
    }

    protected CXDocument Parse(
        [StringSyntax("html")] string cx,
        [StringSyntax("csharp")] string? pretext = null,
        bool allowParsingErrors = false,
        GeneratorOptions? options = null,
        [StringSyntax("csharp")] string? additionalMethods = null,
        string testClassName = "TestClass",
        string testFuncName = "Run",
        bool hasInterpolations = true,
        int quoteCount = 3
    )
    {
        var quotes = new string('"', quoteCount);
        var dollar = hasInterpolations ? "$" : string.Empty;
        var pad = hasInterpolations ? new(' ', dollar.Length) : string.Empty;
        var cxString = new StringBuilder();

        cxString.Append(dollar).Append(quotes);

        if (quoteCount >= 3)
        {
            cxString.AppendLine();
            cxString.Append(pad);
        }

        cxString.Append(quoteCount >= 3 ? cx.WithNewlinePadding(pad.Length) : cx);

        if (quoteCount >= 3)
        {
            cxString.AppendLine();
            cxString.Append(pad);
        }

        cxString.Append(quotes);

        var source =
            $$""""
              using Discord;
              using System.Collections.Generic;
              using System.Linq;

              public class {{testClassName}}
              {
                  public void {{testFuncName}}()
                  {
                      {{pretext}}
                      ComponentDesigner.cx(
                          {{cxString.ToString().WithNewlinePadding(4)}}
                      );
                  }
                  {{additionalMethods?.WithNewlinePadding(4)}}
              }
              """";

        output.WriteLine($"CX:\n{cxString}");

        var token = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        var target = Targets.FromSource(source, token);

        return DoParse(target);
    }

    private CXDocument DoParse(ComponentDesignerTarget target)
    {
        Assert.Empty(_diagnostics);
        Assert.False(_enumerator?.MoveNext() ?? false);

        var token = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        var source = CXSourceText
            .From(target.CX.Designer);

        var reader = source
            .CreateReader(
                target
                    .CX
                    .InterpolationInfos
                    .Select(x => new CXTextSpan(
                        x.Span.Start - target.CX.Location.TextSpan.Start,
                        x.Span.Length
                    ))
                    .ToArray()
            );

        if (_document is null)
        {
            _document = CXParser.Parse(reader, token);
        }
        else
        {
            var changes = new List<CXTextChange>();

            var dmp = new diff_match_patch();
            var diffs = dmp.diff_main(_document.Source!.ToString(), source.ToString());
            dmp.diff_cleanupSemantic(diffs);

            var pos = 0;
            for (var i = 0; i < diffs.Count; i++)
            {
                var diff = diffs[i];
                switch (diff.operation)
                {
                    case Operation.DELETE:
                        if (i < diffs.Count - 1)
                        {
                            var next = diffs[i + 1];
                            if (next.operation is Operation.INSERT)
                            {
                                changes.Add(
                                    new CXTextChange(new(pos, diff.text.Length), next.text)
                                );
                                i++;
                                pos += diff.text.Length;
                                continue;
                            }
                        }

                        changes.Add(new CXTextChange(
                            new(pos, diff.text.Length),
                            string.Empty
                        ));
                        pos += diff.text.Length;
                        break;
                    case Operation.INSERT:
                        changes.Add(new CXTextChange(
                            new(pos, 0),
                            diff.text
                        ));
                        break;
                    case Operation.EQUAL:
                        pos += diff.text.Length;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _document = _document.IncrementalParse(
                reader,
                changes,
                token
            );
        }

        foreach (var diagnostic in _document.AllDiagnostics)
        {
            _diagnostics.Push(diagnostic);
        }

        _enumerator = _document
            .RootNodes
            .SelectMany(Enumerate)
            .GetEnumerator();

        return _document;
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

    protected CXValue.StringLiteral StrLiteral(string? value = null, bool? reused = null)
        => N<CXValue.StringLiteral>(value, reused);

    protected CXElement Element(string? value = null, bool? reused = null)
        => N<CXElement>(value, reused);

    protected CXAttribute Attribute(string? value = null, bool? reused = null)
        => N<CXAttribute>(value, reused);

    protected CXIdentifier SimpleIdent(string value, bool? reused = null)
    {
        var node = N<CXIdentifier.Simple>(value, reused);
        {
            Ident(value, reused);
        }

        return node;
    }
    
    protected CXToken Ident(string value, bool? reused = null)
        => T(CXTokenKind.Identifier, value, reused);

    protected CXToken T(CXTokenKind kind, string? value = null, bool? reused = null)
    {
        var token = N<CXToken>(value, reused);

        Assert.Equal(kind, token.Kind);

        return token;
    }

    protected T N<T>(string? value = null, bool? reused = null) where T : ICXNode
    {
        var current = GetNextNode();

        Assert.IsType<T>(current);

        if (value is not null) Assert.Equal(value, current.ToString());

        if (reused is true)
        {
            Assert.Contains(current, _document!.ReusedNodes);
            Assert.DoesNotContain(current, _document!.NewNodes);
        }
        else if (reused is false)
        {
            Assert.DoesNotContain(current, _document!.ReusedNodes);
            Assert.Contains(current, _document!.NewNodes);
        }

        return (T)current;
    }

    protected CXDiagnostic Diagnostic(CXErrorCode code, string? message = null, CXTextSpan? span = null)
    {
        Assert.NotEmpty(_diagnostics);
        var diagnostic = _diagnostics.Pop();

        Assert.Equal(code, diagnostic.Code);

        if (message is not null) Assert.Equal(message, diagnostic.Message);

        if (span.HasValue) Assert.Equal(span.Value, diagnostic.Span);

        return diagnostic;
    }

    private ICXNode GetNextNode()
    {
        Assert.NotNull(_enumerator);
        Assert.True(_enumerator.MoveNext());

        return _enumerator.Current;
    }

    public void Dispose()
    {
        Assert.NotNull(_enumerator);
        Assert.False(_enumerator.MoveNext());
        Assert.Empty(_diagnostics);
    }
}