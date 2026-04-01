using System.CommandLine;
using System.CommandLine.Parsing;
using ComponentDesigner;

var parseResult = CLI.RootCommand.Parse(args);

await parseResult.InvokeAsync();