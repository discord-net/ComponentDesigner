using ComponentDesigner;

namespace Discord;

internal static class Symbols
{
    extension(ICompilationProvider compilation)
    {
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
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.CXModalComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> CXMessageComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.CXMessageComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> CXComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.CXComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ModalBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ModalBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ModalComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ModalComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ComponentBuilderV2<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ComponentBuilderV2", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> MessageComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.MessageComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IMessageComponent<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.IMessageComponent", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IMessageComponentBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.IMessageComponentBuilder",
            source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SystemUri<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("System.Uri", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> TextDisplayBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.TextDisplayBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> LabelBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.LabelBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> FileUploadComponentBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.FileUploadComponentBuilder",
            source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> TextInputBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.TextInputBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> TextInputStyle<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.TextInputStyle", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ButtonStyle<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ButtonStyle", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ButtonBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.ButtonBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> IEmote<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => compilation.GetSymbol("Discord.IEmote", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SelectMenuDefaultValue<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.SelectMenuDefaultValue", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SelectMenuBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.SelectMenuBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SelectMenuOptionBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.SelectMenuOptionBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ContainerBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.ContainerBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> MediaGalleryBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.MediaGalleryBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> MediaGalleryItemProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.MediaGalleryItemProperties", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ActionRowBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.ActionRowBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> FileBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.FileComponentBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SectionBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.SectionBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> ThumbnailBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.ThumbnailBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SeparatorBuilder<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.SeparatorBuilder", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> SeparatorSpacingSize<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol<ICSharpTypeSymbol>("Discord.SeparatorSpacingSize", source.TextSpan,
                cancellationToken);

        public Result<ICSharpTypeSymbol> UnfurledMediaItemProperties<T>(
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable =>
            compilation.GetSymbol("Discord.UnfurledMediaItemProperties", source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> GetSymbol(
            string name,
            CXTextSpan reference,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>(name, reference, cancellationToken);

        // public Result<ICSharpTypeSymbol> IEnumerableOfIMessageComponentBuilder<T>(
        //     T source,
        //     CancellationToken cancellationToken = default
        // ) where T : ISourceLocatable => compilation
        //     .IMessageComponentBuilder(source, cancellationToken)
        //     .Map(builder =>
        //     {
        //         if (compilation.IEnumerableOfT is not { } enumerableOfT)
        //             return Diagnostic
        //                 .TypeNotFound("IEnumerable`1")
        //                 .At(source);
        //
        //         return new Result<ICSharpTypeSymbol>(enumerableOfT.ConstructGeneric(builder));
        //     });

        public Result<ICSharpTypeSymbol> IEnumerableOf<T>(
            Func<T, CancellationToken, Result<ICSharpTypeSymbol>> symbol,
            T source,
            CancellationToken cancellationToken = default
        ) where T : ISourceLocatable => symbol(source, cancellationToken)
            .Map(symbol =>
            {
                if (compilation.IEnumerableOfT is not { } enumerableOfT)
                    return Diagnostic
                        .TypeNotFound("IEnumerable`1")
                        .At(source);
                return new Result<ICSharpTypeSymbol>(enumerableOfT.ConstructGeneric(symbol));
            });

        public Result<T> GetSymbol<T>(
            string name,
            CXTextSpan reference,
            CancellationToken cancellationToken = default
        ) where T : ICSharpTypeSymbol
        {
            var symbol = compilation.GetTypeFromQualifiedName(name, cancellationToken);

            if (symbol is not T expected)
                return Diagnostic.TypeNotFound(name).At(reference);

            return new(expected);
        }
    }
}