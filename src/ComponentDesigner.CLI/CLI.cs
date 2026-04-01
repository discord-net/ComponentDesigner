using System.CommandLine;
using System.CommandLine.Parsing;
using ComponentDesigner.Commands;
using Discord;

namespace ComponentDesigner;

public static class CLI
{
    private static readonly Dictionary<string, IComponentImplementation> _renderers = new()
    {
        { "dnet", DiscordNetComponentDesignerImplementation.Instance }
    };

    public static readonly Option<IComponentImplementation> RendererOption = new("--renderer", "-r")
    {
        CustomParser = ParseRenderer,
        Validators = { ValidateRenderer },
        Required = true
    };

    public static readonly Option<FileInfo> FileOption = new(
        "--file", "-f"
    )
    {
        Required = true
    };

    public static readonly Command GenerateCommand = new(
        "gen",
        "Generate source from CX"
    )
    {
        Options = { RendererOption },
        Action = new Generate()
    };

    public static readonly Command SyntaxCommand = new(
        "syntax",
        "Validate and introspect the CX syntax"
    )
    {
        Action = new Syntax(),
        Options = { FileOption }
    };


    public static readonly RootCommand RootCommand = new(
        "Run tools related to the component designer and CX language"
    )
    {
        Subcommands = { GenerateCommand, SyntaxCommand }
    };

    private static void ValidateRenderer(OptionResult result)
    {
        if (result.Tokens.Count is not 1)
        {
            result.AddError(
                "Renderer requires a value"
            );
            return;
        }

        var token = result.Tokens[0];

        if (!_renderers.ContainsKey(token.Value))
        {
            result.AddError(
                $"'{token.Value}' is not a valid renderer, valid renderers are: {string.Join(", ", _renderers.Keys.Select(x => $"'{x}'"))}"
            );
            return;
        }
    }

    private static IComponentImplementation? ParseRenderer(ArgumentResult argument)
    {
        if (argument.Tokens.Count is not 1) return null;

        var token = argument.Tokens[0];

        return _renderers.GetValueOrDefault(token.Value);
    }
}