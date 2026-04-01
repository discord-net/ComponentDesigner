using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;
using Spectre.Console;

namespace ComponentDesigner.Commands;

public sealed class Syntax : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var file = parseResult.GetValue(CLI.FileOption);

        if (file is null) return -1;

        if (!file.Exists)
        {
            AnsiConsole.MarkupLine($"[red]'{Markup.Escape(file.ToString())}' doesn't exist");
            return -1;
        }

        var parseStartTime = DateTime.UtcNow;
        
        var (document, highlighter) = await AnsiConsole
            .Status()
            .StartAsync("Parsing document...", async ctx =>
            {
                using var sr = file.OpenText();

                var source = CXSourceText.From(await sr.ReadToEndAsync(cancellationToken));

                var document = CXParser.Parse(
                    source.CreateReader()
                );

                return (document, new SyntaxHighlighter(source));
            });

        var timeToParse = DateTime.UtcNow - parseStartTime;


        AnsiConsole.MarkupLine($"[green]Parsed Document in {timeToParse:g}:[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(highlighter.GetHighlightedSource());
        
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine($"[gray]{document.AllDiagnostics.Count} diagnostics[/]");
        AnsiConsole.MarkupLine($"[gray]{document.Tokens.Count} tokens[/]");
        AnsiConsole.MarkupLine($"[gray]{document.Descendants.Count} nodes[/]");
        
        AnsiConsole.WriteLine();
        
        foreach (var diagnostic in document.AllDiagnostics)
        {
            AnsiConsole.MarkupLine(PrettifyDiagnostic(highlighter, document.Source!, diagnostic));
        }

        return 0;
    }

    private string PrettifyDiagnostic(SyntaxHighlighter highlighter, CXSourceText source, CXDiagnostic diagnostic)
    {
        var startLine = source.Lines.GetLineFromPosition(diagnostic.Span.Start);
        var endLine = source.Lines.GetLineFromPosition(diagnostic.Span.End);

        var lineStart = startLine.LineNumber;
        var columnStart = diagnostic.Span.Start - startLine.Start;

        var lineEnd = endLine.LineNumber;
        var columnEnd = diagnostic.Span.End - endLine.Start;

        var lineNoWidth = lineEnd.ToString().Length;

        var severityColor = GetColorOfSeverity(diagnostic.Severity);
        var errorMessage = new StringBuilder(
                $"[{severityColor}]{diagnostic.Severity.ToString().ToLower()}[[{diagnostic.Code}]][/]: {diagnostic.Message}"
            )
            .AppendLine();

        errorMessage.AppendLine("|".PadLeft(lineNoWidth + 3));

        for (var i = lineStart; i < lineEnd + 1; i++)
        {
            var line = source.Lines[i];
            var lineText = highlighter.GetHighlightedSection(line.Start, line.Length);

            var start = i == lineStart ? columnStart : 0;
            var end = i == lineEnd ? columnEnd : line.Length;

            errorMessage.AppendLine($" {i.ToString().PadLeft(lineNoWidth)} | {lineText}");
            errorMessage
                .Append("|".PadLeft(lineNoWidth + 3))
                .Append(' ')
                .Append($"[{severityColor}]")
                .AppendLine("".PadLeft(end - start, '^').PadLeft(end))
                .Append("[/]");
        }

        return errorMessage.ToString();

        static string GetColorOfSeverity(DiagnosticSeverity severity)
            => severity switch
            {
                DiagnosticSeverity.Error => "red",
                DiagnosticSeverity.Info => "gray",
                DiagnosticSeverity.Warning => "yellow",
                _ => "gray"
            };
    }
}