// using OmniSharp.Extensions.LanguageServer.Protocol.Client;
// using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
// using OmniSharp.Extensions.LanguageServer.Protocol.Document;
// using OmniSharp.Extensions.LanguageServer.Protocol.Models;
//
// namespace Discord.ComponentDesigner.LanguageServer;
//
// public sealed class DiagnosticHandler : DocumentDiagnosticHandlerBase
// {
//     protected override DiagnosticsRegistrationOptions CreateRegistrationOptions(
//         DiagnosticClientCapabilities capability,
//         ClientCapabilities clientCapabilities
//     ) => new()
//     {
//         InterFileDependencies = false,
//         WorkspaceDiagnostics = false,
//         DocumentSelector = DocumentHandler.DocumentSelector
//     };
//
//     public override async Task<RelatedDocumentDiagnosticReport> Handle(
//         DocumentDiagnosticParams request,
//         CancellationToken cancellationToken
//     )
//     {
//         if (!DocumentHandler.TryGetDocument(request.TextDocument.Uri, out var document))
//         {
//             // TODO
//             return new RelatedFullDocumentDiagnosticReport();
//         }
//         
//     }
// }