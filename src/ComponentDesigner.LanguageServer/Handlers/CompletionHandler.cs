using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class CompletionHandler : CompletionHandlerBase
{
    private readonly ILogger<CompletionHandler> _logger;

    public CompletionHandler(ILogger<CompletionHandler> logger)
    {
        _logger = logger;
    }

    protected override CompletionRegistrationOptions CreateRegistrationOptions(
        CompletionCapability capability,
        ClientCapabilities clientCapabilities
    ) => new()
    {
        ResolveProvider = true,
        TriggerCharacters = new(["<"])
    };

    public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Got completion request for {Uri}", request.TextDocument.Uri);
        
        if (!DocumentHandler.TryGetDocument(request.TextDocument.Uri, out var document))
        {
            _logger.LogWarning("Got completion request for unknown document {Uri}", request.TextDocument.Uri);
            return CompletionResult.EmptyCompletionList;
        }

        var graph = document.GetGraph(cancellationToken);
        var offset = document.GetSourceOffsetFromPosition(request.Position);

        var classification = CompletionClassifier
            .Classify(
                graph,
                offset,
                _logger
            );

        if (classification is not null)
        {
            _logger.LogDebug("Classified completion at {Offset}: {Name}", offset, classification.GetType().Name);
        }
        else
        {
            _logger.LogDebug("No classification at {Offset}", offset);
        }
        
        return classification?.ToCompletionList(_logger)
               ?? CompletionResult.EmptyCompletionList;
    }

    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Got completion resolve request for {Label}", request.Label);
        
        return Task.FromResult(request);
    }
}