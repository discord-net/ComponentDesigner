using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes;

public sealed class UnfurledMediaItemGenerator : CXValueGenerator
{
    public override Result<string> Render(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValueGeneratorOptions options
    ) => String(context, target, options)
        .Map(x =>
            $"new {
                context
                    .KnownTypes
                    .UnfurledMediaItemPropertiesType!
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            }({x})"
        );
}