using Discord.ComponentDesigner.LanguageServer.CX;
using ComponentDesigner.Parser;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private enum Modifiers
    {
        None,
        Attribute = 1,
        Element = 2
    }

    private readonly ILogger<SemanticTokensHandler> _logger;

    public SemanticTokensHandler(ILogger<SemanticTokensHandler> logger)
    {
        _logger = logger;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities
    ) => new()
    {
        Legend = new SemanticTokensLegend()
        {
            TokenTypes = new(
                Enum.GetNames<CXTokenKind>()
                    .Select(x => new SemanticTokenType(x))
            ),
            TokenModifiers = new(
                Enum.GetNames<Modifiers>()
                    .Select(x => new SemanticTokenModifier(x))
            )
        },
        Full = true
    };

    protected override Task Tokenize(
        SemanticTokensBuilder builder,
        ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Got tokenize request for {Id}", identifier.TextDocument.Uri);

        if (!ComponentDocument.TryGet(identifier.TextDocument.Uri, out var document))
            return Task.CompletedTask;

        var cx = document.GetCX(cancellationToken);

        foreach (var token in cx.Tokens)
        {
            var startInfo = cx.Source.Lines.GetSourceLocation(token.TextSpan.Start);
            var endInfo = cx.Source.Lines.GetSourceLocation(token.TextSpan.End);

            var modifiers = token.Kind switch
            {
                CXTokenKind.Identifier when token.Parent is CXAttribute => Modifiers.Attribute,
                _ => Modifiers.None
            };
            
            builder.Push(
                new Range(
                    startInfo.Line,
                    startInfo.Column,
                    endInfo.Line,
                    endInfo.Column
                ),
                (int)token.Kind,
                (int)modifiers
            );

            _logger.LogInformation("Token[{Kind}:{Mod}]: {Start} -> {End}", token.Kind, modifiers, startInfo, endInfo);
        }

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params,
        CancellationToken cancellationToken
    ) => Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
}