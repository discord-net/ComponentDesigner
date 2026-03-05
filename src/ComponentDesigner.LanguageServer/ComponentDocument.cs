using ComponentDesigner;
using ComponentDesigner.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Diagnostic = ComponentDesigner.Diagnostic;
using DiagnosticSeverity = ComponentDesigner.DiagnosticSeverity;
using LSPDiagnostic = OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic;
using LSPDiagnosticSeverity = OmniSharp.Extensions.LanguageServer.Protocol.Models.DiagnosticSeverity;
using LSPRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Discord.ComponentDesigner.LanguageServer.CX;

public sealed class ComponentDocument
{
    //public const bool FORCE_NO_INCREMENTAL = true;

    public DocumentUri Uri { get; }

    public int? Version { get; }

    public CXSourceText Source { get; }

    private readonly CXModel _cxModel;
    private CXDocument? _document;
    private CXComponentGraph? _graph;

    public ComponentDocument(
        DocumentUri uri,
        string source,
        int? version
    ) : this(uri, CXSourceText.From(source), version)
    {
    }

    public ComponentDocument(
        DocumentUri uri,
        CXSourceText source,
        int? version
    )
    {
        Uri = uri;
        Version = version;
        Source = source;
        _cxModel = new(
            source.ToString(),
            new(
                uri.Path,
                new(0, Source.Length),
                new(
                    new(0, 0),
                    Source.Lines.GetLinePositon(Math.Max(0, Source.Length - 1))
                )
            ),
            3,
            false,
            "designer",
            []
        );
    }

    public ComponentDocument WithChanges(
        int? version,
        IEnumerable<TextDocumentContentChangeEvent> changes
    )
    {
        var newSource = Source.WithChanges([
            ..changes.Select(x =>
                new CXTextChange(
                    ToTextSpan(x.Range!),
                    x.Text
                )
            )
        ]);

        return new(Uri, newSource, version);


        CXTextSpan ToTextSpan(LSPRange range)
            => CXTextSpan.FromBounds(
                Source.Lines[range.Start.Line].Start + range.Start.Character,
                Source.Lines[range.End.Line].Start + range.End.Character
            );
    }

    public CXComponentGraph GetGraph(CancellationToken cancellationToken)
        => _graph ??= CXComponentGraph.Create(
            new GraphParameters(
                LanguageServerComponentImplementation.Instance,
                LSPCompilationProvider.Instance,
                _cxModel,
                new LSPGraphOptions(false, false)
            ),
            GetParsedSource(cancellationToken)
        );

    public CXDocument GetParsedSource(CancellationToken cancellationToken)
        => _document ??= CXParser.Parse(Source.CreateReader(), cancellationToken);

    public IReadOnlyList<LSPDiagnostic> GetDiagnostics(CancellationToken cancellationToken)
    {
        var graph = GetGraph(cancellationToken);

        IReadOnlyList<Diagnostic> diagnostics = [
            ..graph.Diagnostics, 
            ..graph.Validate(LSPCompilationProvider.Instance, cancellationToken)
        ];
        
        
        return [..diagnostics.Distinct().Select(ConvertDiagnostic)];

        LSPDiagnostic ConvertDiagnostic(Diagnostic diagnostic)
            => new()
            {
                Code = new(diagnostic.Id),
                Severity = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => LSPDiagnosticSeverity.Error,
                    DiagnosticSeverity.Info => LSPDiagnosticSeverity.Information,
                    DiagnosticSeverity.Warning => LSPDiagnosticSeverity.Warning,
                    _ => LSPDiagnosticSeverity.Hint,
                },
                Message = diagnostic.Title,
                Range = GetRange(diagnostic.TextSpan),
            };
    }

    public LSPRange GetRange(CXTextSpan span)
    {
        var start = Source.Lines.GetLinePositon(span.Start);
        var end = Source.Lines.GetLinePositon(span.End);

        return new()
        {
            Start = new(start.Line, start.Character),
            End = new(end.Line, end.Character)
        };
    }
}