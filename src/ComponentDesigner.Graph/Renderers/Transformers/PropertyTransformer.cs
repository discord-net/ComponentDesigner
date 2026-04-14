using System.Globalization;
using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public static class PropertyTransformer
{
    public static readonly ComponentPropertyValueTransformer<string> String = TransformToString;
    public static readonly ComponentPropertyValueTransformer<uint> ColorCode = TransformToColorCode;
    public static readonly ComponentPropertyValueTransformer<PartialEmoji> PartialEmoji = TransformToPartialEmoji;

    public static Result<T> Switch<T>(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken,
        ComponentPropertyValueTransformer<T, ComponentPropertyValue.Literal>? literal = null,
        ComponentPropertyValueTransformer<T, ComponentPropertyValue.Component>? component = null,
        ComponentPropertyValueTransformer<T, ComponentPropertyValue.Interpolation>? interpolation = null,
        ComponentPropertyValueTransformer<T, ComponentPropertyValue.Many>? many = null,
        ComponentPropertyValueTransformer<T, ComponentPropertyValue.None>? none = null
    )
    {
        var handledKinds = ComponentPropertyValueKind.None;

        if (literal is not null)
            handledKinds |= ComponentPropertyValueKind.Literal;

        if (component is not null)
            handledKinds |= ComponentPropertyValueKind.Component;

        if (interpolation is not null)
            handledKinds |= ComponentPropertyValueKind.Interpolation;

        if (many is not null)
            handledKinds |= ComponentPropertyValueKind.Many;

        return Core(value);

        Result<T> Core(ComponentPropertyValue value)
        {
            switch (value)
            {
                case ComponentPropertyValue.Literal literalValue when literal is not null:
                    return literal(context, literalValue, cancellationToken);

                case ComponentPropertyValue.Component componentValue when component is not null:
                    return component(context, componentValue, cancellationToken);

                case ComponentPropertyValue.Interpolation interpolationValue when interpolation is not null:
                    return interpolation(context, interpolationValue, cancellationToken);

                case ComponentPropertyValue.Many manyValue:
                    return many?.Invoke(context, manyValue, cancellationToken) ?? DefaultManyHandler(manyValue);

                case ComponentPropertyValue.None noneValue when none is not null:
                    return none(context, noneValue, cancellationToken);

                default:
                    return Diagnostic
                        .InvalidPropertyValue(value, handledKinds)
                        .At(value);
            }
        }

        Result<T> DefaultManyHandler(
            ComponentPropertyValue.Many manyValue
        )
        {
            if (manyValue.AsSingle is { } innerValue) return Core(innerValue);

            return Diagnostic.InvalidPropertyValue(manyValue, handledKinds).At(manyValue);
        }
    }


    private static Result<string> TransformToString(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    )
    {
        return Switch(
            context,
            value,
            cancellationToken,
            literal: static (_, literal, _) => literal.Value,
            interpolation: TransformInterpolation
        );

        static Result<string> TransformInterpolation(
            IRendererContext context,
            ComponentPropertyValue.Interpolation value,
            CancellationToken cancellationToken = default
        )
        {
            if (value.Info.ConstantValue.IsSpecified)
                return value.Info.ConstantValue.ToString() ?? string.Empty;

            return Diagnostic.ExpectedAConstantValue.At(value);
        }
    }

    private static Result<uint> TransformToColorCode(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    ) => TransformToString(context, value, cancellationToken)
        .Map(Result<uint> (str) =>
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (str.StartsWith("#"))
                    str = str.Substring(1);

                if (uint.TryParse(str, NumberStyles.HexNumber, null, out var hexCode))
                    return hexCode;
            }

            return Diagnostic.NotAColorCode(str).At(value);
        });

    private static Result<PartialEmoji> TransformToPartialEmoji(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    ) => TransformToString(context, value, cancellationToken)
        .Map(Result<PartialEmoji> (str) =>
        {
            if (ComponentDesigner.PartialEmoji.TryParse(str, out var partialEmoji))
                return partialEmoji;

            return Diagnostic.NotAnEmoji(str).At(value);
        });
}