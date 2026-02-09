using ComponentDesigner.Parser;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Serilog;

namespace Discord.ComponentDesigner.LanguageServer.CX;

public sealed class ComponentDocument
{
    public const bool FORCE_NO_INCREMENTAL = true;
    
    private static readonly Dictionary<DocumentUri, ComponentDocument> _documents = [];

    public DocumentUri Uri { get; }

    public int? Version { get; }

    private CXSourceText _source;

    private CXTextSpan? _incrementalChangeRange;

    private CXDocument? _cxDoc;

    public ComponentDocument(
        DocumentUri uri,
        string source,
        int? version
    )
    {
        Uri = uri;
        Version = version;
        _source= CXSourceText.From(source);
    }

    public CXDocument GetCX(CancellationToken cancellationToken = default)
    {
        if (_cxDoc?.Source == _source) return _cxDoc;
        
        var reader = new CXSourceReader(
            _source,
            [],
            3
        );

        return _cxDoc = CXParser.Parse(reader, cancellationToken);
    }

    public static ComponentDocument Create(
        DocumentUri uri,
        string content,
        int? version,
        CancellationToken token
    ) => _documents[uri] = new(uri, content, version);

    public void Update(
        int? version,
        Container<TextDocumentContentChangeEvent> changes,
        CancellationToken token
    )
    {
        if (Version.HasValue && Version == version) return;

        var sourceChanges = new List<CXTextChange>();

        foreach (var change in changes)
        {
            if (change.Range is null) return;

            var start = _source.Lines[change.Range.Start.Line].Start + change.Range.Start.Character;
            var end = _source.Lines[change.Range.End.Line].Start + change.Range.End.Character;

            var newChange = new CXTextChange(CXTextSpan.FromBounds(start, end), change.Text);
            sourceChanges.Add(newChange); 
            
            Log.Logger.Information("[{Id}]: Change: {Change}", Uri, newChange);
        }

        var newSource = _source.WithChanges(sourceChanges);
        
        Log.Logger.Information("[{Id}]: Update\nOld:\n{Old}\n\nNew:\n{New}", Uri, _source, newSource);

        foreach (var change in changes)
        {
            Log.Logger.Information("Change: {Change}", change);
        }

        _source = newSource;
    }

    public void Close()
    {
        _documents.Remove(Uri);
    }

    public static bool TryGet(DocumentUri uri, [MaybeNullWhen(false)] out ComponentDocument document)
        => _documents.TryGetValue(uri, out document);
}
