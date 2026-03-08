using Discord.ComponentDesigner.LanguageServer;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        standardErrorFromLevel: LogEventLevel.Verbose,
        outputTemplate: "{Timestamp:HH:mm:ss} | {Level} - [{SourceContext}]: {Message:lj}{NewLine}{Exception}"
    )
    .MinimumLevel.Debug()
    .CreateLogger();

Log.Logger.Information("Starting LSP server...");


var server = await LanguageServer
    .From(options => options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .ConfigureLogging(x => x
            .AddSerilog(Log.Logger)
            .AddLanguageProtocolLogging()
            .SetMinimumLevel(LogLevel.Debug)
        )
        .AddHandler<CompletionHandler>()
        .AddHandler<DocumentHandler>()
        .AddHandler<HoverHandler>()
        .OnInitialize((languageServer, request, token) =>
        {
            Log.Logger.Information("Server is initializing...");
            return Task.CompletedTask;
        })
        .OnInitialized((languageServer, request, response, token) =>
        {
            Log.Logger.Information("Server initialized! Capabilities:\n{Res}", response.Capabilities);
            return Task.CompletedTask;
        })
        .OnStarted((languageServer, token) =>
        {
            Log.Logger.Information("Server started!");
            return Task.CompletedTask;
        })
    )
    .ConfigureAwait(false);

await server.WaitForExit.ConfigureAwait(false);