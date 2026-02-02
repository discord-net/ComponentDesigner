using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public abstract class CSharpValueGenerator
{
    public virtual Result<string> Render(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.Value switch
    {
        CXValue.Scalar scalar => RenderScalar(context, target, scalar.Token, options, cancellationToken),
        CXValue.Interpolation interpolation => RenderInterpolation(
            context,
            target,
            interpolation.Token,
            context.GetInterpolationInfo(interpolation),
            options, 
            cancellationToken
        ),
        CXValue.StringLiteral stringLiteral => RenderStringLiteral(context, target, stringLiteral, options, cancellationToken),
        CXValue.Multipart multipart => ExtrapolateAndRenderMultipart(context, target, multipart, options, cancellationToken),
        CXValue.Element element => RenderElementValue(context, target, element, options, cancellationToken),
        _ => RenderMissingValue(context, target, options, cancellationToken)
    };

    private Result<string> ExtrapolateAndRenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (multipart is { HasInterpolations: false, Tokens.Count: 1 })
            return RenderScalar(context, target, multipart.Tokens[0], options, cancellationToken);

        if (multipart.TryGetSingleInterpolation(context, out var info))
            return RenderInterpolation(context, target, multipart.Tokens[0], info, options, cancellationToken);

        return RenderMultipart(context, target, multipart, options, cancellationToken);
    }

    protected virtual Result<string> RenderElementValue(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Element element,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated(element)
    );

    protected virtual Result<string> RenderMissingValue(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated("unknown")
    );

    protected virtual Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) =>target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated("scalar")
    );

    protected virtual Result<string> RenderInterpolation(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        IInterpolationInfo info,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated("interpolation")
    );

    protected virtual Result<string> RenderStringLiteral(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.StringLiteral stringLiteral,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => ExtrapolateAndRenderMultipart(context, target, stringLiteral, options, cancellationToken);

    protected virtual Result<string> RenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated(multipart)
    );
}