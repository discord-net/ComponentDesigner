using ComponentDesigner;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Discord.ComponentDesigner.LanguageServer;

[Parallel, Method("cx/preview-json", Direction.ClientToServer)]
public class JsonPreviewRequest : IRequest<JsonPreviewResponse>
{
    public DocumentUri Uri { get; set; }
}

public sealed record JsonPreviewResponse(string Json);

public sealed class JsonPreviewHandler : IJsonRpcRequestHandler<JsonPreviewRequest, JsonPreviewResponse>
{
    private readonly ILogger<JsonPreviewHandler> _logger;
    private readonly ILanguageServerFacade _server;
    public JsonPreviewHandler(ILanguageServerFacade server, ILogger<JsonPreviewHandler> logger)
    {
        _logger = logger;
        _server = server;
    }
    
    public async Task<JsonPreviewResponse> Handle(JsonPreviewRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GOT JSON PREVIEW REQUEST FOR {Uri}", request.Uri);

        if (!DocumentHandler.TryGetDocument(request.Uri, out var doc))
        {
            _logger.LogWarning("No document found for {Uri}", request.Uri);
            return new JsonPreviewResponse("// no doc found");
        }

        var result = doc.GetJson(cancellationToken);

        _server.TextDocument.PublishDiagnostics(
            new()
            {
                Uri = doc.Uri,
                Diagnostics = new(result.Diagnostics.Distinct().Select(doc.ConvertDiagnostic))
            }
        );

        var preview = result.GetValueOrDefault("// err");
        
        _logger.LogDebug("Preview:\n{Prev}", preview);
        
        return new JsonPreviewResponse(preview);
    }
}