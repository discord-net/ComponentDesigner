using ComponentDesigner;

namespace Discord;

public sealed class UnfurledMediaItemGenerator : CSharpValueGenerator
{
    public static readonly UnfurledMediaItemGenerator Instance = new();

    public override Result<string> Render(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CSharpValueGeneratorOptions options = default,
        CancellationToken cancellationToken = default
    ) => String
        .Render(context, target, options, cancellationToken)
        .Combine(
            context.CompilationProvider.UnfurledMediaItemProperties(target.TextSpan, cancellationToken),
            (url, symbol) => $"new {symbol.ToQualifiedName()}({url})"
        );
}