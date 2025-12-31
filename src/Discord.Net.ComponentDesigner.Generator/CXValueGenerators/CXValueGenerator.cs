using System;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public delegate Result<string> CXValueGeneratorDelegate(
    IComponentContext context,
    CXValueGeneratorTarget target,
    CXValueGeneratorOptions options
);

public abstract class CXValueGenerator
{
    public static CXValueGeneratorDelegate Boolean => GetGenerator<BooleanGenerator>().Render;
    public static CXValueGeneratorDelegate Color => GetGenerator<ColorGenerator>().Render;
    public static CXValueGeneratorDelegate Component => GetGenerator<ComponentGenerator>().Render;
    public static CXValueGeneratorDelegate Emoji => GetGenerator<EmojiGenerator>().Render;
    public static CXValueGeneratorDelegate Integer => GetGenerator<IntegerGenerator>().Render;
    public static CXValueGeneratorDelegate Snowflake => GetGenerator<SnowflakeGenerator>().Render;
    public static CXValueGeneratorDelegate String => GetGenerator<StringGenerator>().Render;

    public static CXValueGeneratorDelegate UnfurledMediaItem
        => GetGenerator<UnfurledMediaItemGenerator>().Render;

    public static CXValueGeneratorDelegate Enum(string qualifiedEnumName, bool renderAsSymbolReference = true)
        => EnumGenerator.Create(qualifiedEnumName, renderAsSymbolReference).Render;

    private static readonly Dictionary<Type, CXValueGenerator> _renderers;

    static CXValueGenerator()
    {
        _renderers = typeof(CXValueGenerator)
            .Assembly
            .GetTypes()
            .Where(x =>
                !x.IsAbstract &&
                x.BaseType == typeof(CXValueGenerator) &&
                x.GetConstructor(Type.EmptyTypes) is not null
            )
            .ToDictionary(
                x => x,
                x => (CXValueGenerator)Activator.CreateInstance(x)
            );
    }

    public static T GetGenerator<T>() where T : CXValueGenerator
        => (T)_renderers[typeof(T)];

    public static CXValueGeneratorDelegate GetGeneratorForType(
        Compilation compilation,
        ITypeSymbol symbol,
        bool allowComponents = false
    )
    {
        switch (symbol.SpecialType)
        {
            case SpecialType.System_String: return String;
            case SpecialType.System_Int32: return Integer;
            case SpecialType.System_UInt64: return Snowflake;
        }

        var knownTypes = compilation.GetKnownTypes();

        if (
            knownTypes.ColorType?.Equals(symbol, SymbolEqualityComparer.Default) ?? false
        ) return Color;

        if (
            knownTypes.IEmoteType?.Equals(symbol, SymbolEqualityComparer.Default) ?? false
        ) return Emoji;

        if (symbol.TypeKind is TypeKind.Enum)
            return Enum(symbol.ToDisplayString());

        if (allowComponents && ComponentBuilderKind.IsValidComponentBuilderType(symbol, compilation))
            return Component;


        return new InterpolationGenerator(symbol).Render;
    }

    public static Result<string> Default(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValueGeneratorOptions options
    ) => "default";

    public virtual Result<string> Render(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValueGeneratorOptions options
    ) => target.Value switch
    {
        CXValue.Scalar scalar => RenderScalar(context, target, scalar.Token, options),
        CXValue.Interpolation interpolation => RenderInterpolation(
            context,
            target,
            interpolation.Token,
            context.GetInterpolationInfo(interpolation),
            options
        ),
        CXValue.StringLiteral stringLiteral => RenderStringLiteral(context, target, stringLiteral, options),
        CXValue.Multipart multipart => ExtrapolateAndRenderMultipart(context, target, multipart, options),
        CXValue.Element element => RenderElementValue(context, target, element, options),
        _ => RenderMissingValue(context, target, options)
    };

    private Result<string> ExtrapolateAndRenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    )
    {
        if (multipart is { HasInterpolations: false, Tokens.Count: 1 })
            return RenderScalar(context, target, multipart.Tokens[0], options);

        if (multipart.IsLoneInterpolatedLiteral(context, out var info))
            return RenderInterpolation(context, target, multipart.Tokens[0], info, options);

        return RenderMultipart(context, target, multipart, options);
    }

    protected virtual Result<string> RenderElementValue(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Element element,
        CXValueGeneratorOptions options
    ) => new DiagnosticInfo(
        Diagnostics.TypeMismatch("value", "element"),
        target.Span
    );

    protected virtual Result<string> RenderMissingValue(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValueGeneratorOptions options
    ) => new DiagnosticInfo(
        Diagnostics.TypeMismatch("value", "missing"),
        target.Span
    );

    protected virtual Result<string> RenderScalar(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        CXValueGeneratorOptions options
    ) => new DiagnosticInfo(
        Diagnostics.InvalidValue("scalar"),
        token
    );

    protected virtual Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    ) => new DiagnosticInfo(
        Diagnostics.InvalidValue("interpolation"),
        token
    );

    protected virtual Result<string> RenderStringLiteral(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.StringLiteral stringLiteral,
        CXValueGeneratorOptions options
    ) => ExtrapolateAndRenderMultipart(context, target, stringLiteral, options);

    protected virtual Result<string> RenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    ) => new DiagnosticInfo(
        Diagnostics.InvalidValue("multipart"),
        multipart
    );
}