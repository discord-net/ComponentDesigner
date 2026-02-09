namespace ComponentDesigner.Renderers.DiscordNet;

partial class DiscordNetComponentRenderer
{
    private static DiagnosticDescriptor MissingRequiredSymbol(string name)
        => new(
            "DNET01",
            DiagnosticSeverity.Error,
            $"Missing symbol '{name}'",
            $"'{name}' couldn't be found in your compilation"
        );
}