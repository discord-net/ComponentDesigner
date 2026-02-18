using ComponentDesigner;

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

        public static Result<CSharpValueGenerator> SeparatorSpacingSize(
            ICompilationProvider compilationProvider,
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default,
            bool allowNullable = false
        ) => compilationProvider
            .SeparatorSpacingSize(textSpan, cancellationToken)
            .Map(CSharpValueGenerator (symbol) =>
                EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable)
            );
        
        public static Result<CSharpValueGenerator> ButtonStyle(
            ICompilationProvider compilationProvider,
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default,
            bool allowNullable = false
        ) => compilationProvider
            .ButtonStyle(textSpan, cancellationToken)
            .Map(CSharpValueGenerator (symbol) =>
                EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable)
            );
        
        public static Result<CSharpValueGenerator> TextInputStyle(
            ICompilationProvider compilationProvider,
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default,
            bool allowNullable = false
        ) => compilationProvider
            .TextInputStyle(textSpan, cancellationToken)
            .Map(CSharpValueGenerator (symbol) =>
                EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable)
            );
    }
}