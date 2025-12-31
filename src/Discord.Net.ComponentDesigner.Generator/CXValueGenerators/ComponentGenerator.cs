using Discord.CX.Nodes.Components;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public sealed class ComponentGenerator : CXValueGenerator
{
    public override Result<string> Render(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValueGeneratorOptions options
    )
    {
        if (target is not CXValueGeneratorTarget.ComponentProperty(var property))
        {
            return new DiagnosticInfo(
                Diagnostics.InvalidValue(target.GetType().Name),
                target.Span
            );
        }

        if (property.GraphNode is null)
        {
            return new DiagnosticInfo(
                Diagnostics.InvalidPropertyValueSyntax("interpolation/element"),
                target.Span
            );
        }

        return base.Render(context, target, options);
    }

    protected override Result<string> RenderElementValue(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Element element,
        CXValueGeneratorOptions options
    )
    {
        if (target is not CXValueGeneratorTarget.ComponentProperty { Property.GraphNode: { } graphNode })
            return new DiagnosticInfo(
                Diagnostics.InvalidPropertyValueSyntax("element"),
                element
            );

        return graphNode.Render(context, options);
    }

    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (
            !ComponentBuilderKind.IsValidComponentBuilderType(
                info.Symbol,
                context.Compilation,
                out var kind
            )
        )
        {
            return new DiagnosticInfo(
                Diagnostics.TypeMismatch(
                    "component",
                    info.Symbol?.ToDisplayString() ?? "unknown"
                ),
                token
            );
        }

        return kind.Conform(
            context.GetDesignerValue(info, info.Symbol),
            options.TypingContext ?? ComponentTypingContext.SingleBuilder,
            token
        );
    }
}