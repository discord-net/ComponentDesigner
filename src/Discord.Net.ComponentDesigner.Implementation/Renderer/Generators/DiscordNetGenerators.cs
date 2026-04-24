using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

public static class DiscordNetGenerators
{
    extension(CSharpValueGenerator)
    {
        public static CSharpValueGenerator UnfurledMediaItemProperties => UnfurledMediaItemGenerator.Instance;
        public static CSharpValueGenerator Color => ColorGenerator.Get(allowNullable: false);
        public static CSharpValueGenerator NullableColor => ColorGenerator.Get(allowNullable: true);
        public static CSharpValueGenerator Emoji => EmojiGenerator.Get(allowNullable: false);
        public static CSharpValueGenerator NullableEmoji => EmojiGenerator.Get(allowNullable: true);

        public static Result<CSharpRender> ButtonStyle(
            IRenderContext context,
            ComponentPropertyValue value,
            CancellationToken cancellationToken = default
        ) => context
            .CompilationProvider
            .ButtonStyle(value.TextSpan, cancellationToken)
            .Map(symbol => EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: false))
            .Map(generator => generator.Render(context, value, cancellationToken));

        public static Result<CSharpRender> SeparatorSpacingSize(
            IRenderContext context,
            ComponentPropertyValue value,
            CancellationToken cancellationToken = default
        ) => context
            .CompilationProvider
            .SeparatorSpacingSize(value.TextSpan, cancellationToken)
            .Map(symbol => EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: false))
            .Map(generator => generator.Render(context, value, cancellationToken));


        public static Result<CSharpRender> TextInputStyle(
            IRenderContext context,
            ComponentPropertyValue value,
            CancellationToken cancellationToken = default
        ) => context
            .CompilationProvider
            .TextInputStyle(value.TextSpan, cancellationToken)
            .Map(symbol => EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: false))
            .Map(generator => generator.Render(context, value, cancellationToken));
    }
}