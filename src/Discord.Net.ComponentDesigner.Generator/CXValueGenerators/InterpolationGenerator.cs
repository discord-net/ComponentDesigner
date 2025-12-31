using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class InterpolationGenerator(ITypeSymbol target) : CXValueGenerator
{
    public ITypeSymbol Target { get; } = target;

    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (
            !context.Compilation.HasImplicitConversion(
                info.Symbol,
                Target
            )
        )
        {
            return new DiagnosticInfo(
                Diagnostics.TypeMismatch(Target.ToDisplayString(), info.Symbol?.ToDisplayString() ?? "unknown"),
                token
            );
        }

        return context.GetDesignerValue(info, Target);
    }
}