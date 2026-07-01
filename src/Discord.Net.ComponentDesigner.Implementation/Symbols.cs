using System.Diagnostics.CodeAnalysis;
using ComponentDesigner;

namespace Discord;

internal static class Symbols
{
    public delegate Result<ICSharpTypeSymbol> Fetch<in T>(T source, CancellationToken cancellationToken = default)
        where T : ISourceLocatable;
    
    extension(ICSharpTypeSymbol? symbol)
    {
        public bool Equals(
            Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> func,
            CancellationToken cancellationToken
        ) => func(default, cancellationToken).GetValueOrDefault()?.Equals(symbol) is true;
        
        public bool Equals(
            Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> func,
            [MaybeNullWhen(false)]out ICSharpTypeSymbol target,
            CancellationToken cancellationToken
        ) => (target = func(default, cancellationToken).GetValueOrDefault())?.Equals(symbol) is true;
    }

    extension(ICompilationProvider compilation)
    {
        public Result<ICSharpTypeSymbol> ChannelType<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ChannelType", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> Color<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.Color", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> CheckboxGroupBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.CheckboxGroupBuilder", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> CheckboxGroupOptionProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.CheckboxGroupOptionProperties", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> CheckboxBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.CheckboxBuilder", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> RadioGroupBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.RadioGroupBuilder", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> RadioGroupOptionProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.RadioGroupOptionProperties", source.TextSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> RefBox<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable
            => compilation.GetTypeSymbol("ComponentDesigner.RefBox`1", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IEnumerableOfIMessageComponentBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable
            => compilation.IEnumerableOf(compilation.IMessageComponentBuilder, source, cancellationToken);

        public Result<ICSharpTypeSymbol> IEnumerableOfMediaGalleryItemProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable
            => compilation.IEnumerableOf(compilation.MediaGalleryItemProperties, source, cancellationToken);

        public Result<ICSharpTypeSymbol> CXModalComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.CXModalComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> CXMessageComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.CXMessageComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> CXComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.CXComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ModalBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ModalBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ModalComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ModalComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ComponentBuilderV2<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ComponentBuilderV2", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> MessageComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.MessageComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IMessageComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.IMessageComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IMessageComponentBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => compilation.GetTypeSymbol("Discord.IMessageComponentBuilder",
            source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SystemUri<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("System.Uri", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> TextDisplayBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.TextDisplayBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> LabelBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.LabelBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> FileUploadComponentBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => compilation.GetTypeSymbol("Discord.FileUploadComponentBuilder",
            source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> TextInputBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.TextInputBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> TextInputStyle<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.TextInputStyle", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ButtonStyle<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ButtonStyle", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ButtonBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ButtonBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IEmote<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => compilation.GetTypeSymbol("Discord.IEmote", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SelectMenuDefaultValue<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.SelectMenuDefaultValue", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SelectMenuBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.SelectMenuBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SelectMenuOptionBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.SelectMenuOptionBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ContainerBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ContainerBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> MediaGalleryBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.MediaGalleryBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> MediaGalleryItemProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.MediaGalleryItemProperties", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ActionRowBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ActionRowBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> FileBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.FileComponentBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SectionBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.SectionBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ThumbnailBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.ThumbnailBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SeparatorBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.SeparatorBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SeparatorSpacingSize<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.SeparatorSpacingSize", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> UnfurledMediaItemProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetTypeSymbol("Discord.UnfurledMediaItemProperties", source.TextSpan, cancellationToken);
        public Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> IEnumerableOf(
            Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> symbol
        ) => (source, cancellationToken) => compilation.IEnumerableOf(
            symbol,
            source,
            cancellationToken
        );
        
        public Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> ListOf(
            Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> symbol
        ) => (source, cancellationToken) => compilation.ListOf(
            symbol,
            source,
            cancellationToken
        );

        public Result<ICSharpTypeSymbol> IEnumerableOf<T>(
            Func<T, CancellationToken, Result<ICSharpTypeSymbol>> symbol,
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable
            => compilation.IEnumerableOfT(source, cancellationToken)
                .Combine(
                    symbol(source, cancellationToken),
                    (enumerableSymbol, innerSymbol) =>
                        enumerableSymbol.ConstructGeneric(innerSymbol)
                );
        
        public Result<ICSharpTypeSymbol> ListOf<T>(
            Func<T, CancellationToken, Result<ICSharpTypeSymbol>> symbol,
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable
            => compilation.ListOfT(source, cancellationToken)
                .Combine(
                    symbol(source, cancellationToken),
                    (enumerableSymbol, innerSymbol) =>
                        enumerableSymbol.ConstructGeneric(innerSymbol)
                );
        
    }
}