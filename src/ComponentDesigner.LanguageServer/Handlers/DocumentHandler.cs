using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ComponentDesigner;
using ComponentDesigner.Parser;
using Discord.ComponentDesigner.LanguageServer.CX;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

using LSPRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class DocumentHandler : TextDocumentSyncHandlerBase
{
    private static readonly ConcurrentDictionary<DocumentUri, ComponentDocument> _documents = [];

    private readonly ILogger<DocumentHandler> _logger;
    private readonly ILanguageServerFacade _server;

    public static readonly TextDocumentSelector DocumentSelector = new(
        new TextDocumentFilter { Pattern = "**/*.cx" }
    );

    public DocumentHandler(
        ILogger<DocumentHandler> logger,
        ILanguageServerFacade server
    )
    {
        _logger = logger;
        _server = server;
    }


    public static bool TryGetDocument(DocumentUri uri, [MaybeNullWhen(false)] out ComponentDocument document)
        => _documents.TryGetValue(uri, out document);

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        => new(uri, "cx");

    private void PublishDiagnosticsForDocument(ComponentDocument document, CancellationToken cancellationToken)
    {
        var diagnostics = document.GetDiagnostics(cancellationToken);

        for (var i = 0; i < diagnostics.Count; i++)
        {
            var diagnostic = diagnostics[i];
            _logger.LogDebug("Diagnostic #{Num}: {Diag}", i + 1, diagnostic);
        }

        _server.TextDocument.PublishDiagnostics(
            new()
            {
                Diagnostics = new(diagnostics),
                Uri = document.Uri
            }
        );
    }

    private void PublishPreview(ComponentDocument document, CancellationToken cancellationToken)
    {
        var result = document.GetJson(cancellationToken);
        
        _server.TextDocument.PublishDiagnostics(
            new()
            {
                Uri = document.Uri,
                Diagnostics = new(result.Diagnostics.Distinct().Select(document.ConvertDiagnostic))
            }
        );

        var preview = result.GetValueOrDefault("// err");

        _server.SendNotification("cx/preview-json", new
        {
            json = preview,
            uri = document.Uri
        });
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Opening {Uri}", request.TextDocument.Uri);

        var document = new ComponentDocument(
            request.TextDocument.Uri,
            request.TextDocument.Text,
            request.TextDocument.Version
        );

        _documents[request.TextDocument.Uri] = document;

        PublishPreview(document, cancellationToken);

        return Unit.Task;
    }

    public override Task<Unit> Handle(
        DidChangeTextDocumentParams request,
        CancellationToken cancellationToken
    )
    {
        if (!_documents.TryGetValue(request.TextDocument.Uri, out var document))
        {
            _logger.LogWarning("Unknown document update {Uri}", request.TextDocument.Uri);
            return Unit.Task;
        }

        _logger.LogInformation("Updating {Uri}, version {Ver}", request.TextDocument.Uri, request.TextDocument.Version);

        // document = document.WithChanges(
        //     request.TextDocument.Version,
        //     request.ContentChanges
        // );
        var changes = new List<CXTextChange>();

        foreach (var change in request.ContentChanges)
        {
            var changeSpan = ToTextSpan(change.Range!);
            var cxChange = new CXTextChange(
                changeSpan,
                change.Text
            );
            
            changes.Add(cxChange);
            
            _logger.LogDebug("Change #{Num}: {Change}", changes.Count + 1, cxChange);
        }
        
        var newSource = document.Source.WithChanges(changes);

        _logger.LogDebug("New source:\n{Source}", newSource.ToString());
        
        document = new(document.Uri, newSource, request.TextDocument.Version);
        _documents[document.Uri] = document;

        PublishPreview(document, cancellationToken);

        return Unit.Task;
        
        CXTextSpan ToTextSpan(LSPRange range)
            => CXTextSpan.FromBounds(
                document.Source.Lines[range.Start.Line].Start + range.Start.Character,
                document.Source.Lines[range.End.Line].Start + range.End.Character
            );
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        => Unit.Task;

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Got close request for {Uri}", request.TextDocument.Uri);

        _documents.Remove(request.TextDocument.Uri, out _);
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities
    ) => new TextDocumentSyncRegistrationOptions()
    {
        Change = TextDocumentSyncKind.Incremental,
        Save = false
    };
}