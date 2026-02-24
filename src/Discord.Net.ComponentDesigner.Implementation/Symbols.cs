using ComponentDesigner;

namespace Discord;

internal static class Symbols
{

    extension(ICompilationProvider compilation)
    {
        public Result<ICSharpTypeSymbol> CXModalComponent(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.CXModalComponent", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> CXMessageComponent(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.CXMessageComponent", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> CXComponent(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.CXComponent", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ModalBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ModalBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ModalComponent(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ModalComponent", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ComponentBuilderV2(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ComponentBuilderV2", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> MessageComponent(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.MessageComponent", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> IMessageComponent(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.IMessageComponent", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> IMessageComponentBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.IMessageComponentBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SystemUri(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("System.Uri", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> TextDisplayBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.TextDisplayBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> LabelBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.LabelBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> FileUploadComponentBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.FileUploadComponentBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> TextInputBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.TextInputBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> TextInputStyle(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.TextInputStyle", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ButtonStyle(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.ButtonStyle", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ButtonBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.ButtonBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> IEmote(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.IEmote", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SelectMenuDefaultValue(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.SelectMenuDefaultValue", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SelectMenuBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.SelectMenuBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SelectMenuOptionBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.SelectMenuOptionBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ContainerBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.ContainerBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> MediaGalleryBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.MediaGalleryBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> MediaGalleryItemProperties(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.MediaGalleryItemProperties", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ActionRowBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.ActionRowBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> FileBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.FileComponentBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SectionBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.SectionBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> ThumbnailBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.ThumbnailBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SeparatorBuilder(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.SeparatorBuilder", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> SeparatorSpacingSize(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>("Discord.SeparatorSpacingSize", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> UnfurledMediaItemProperties(
            CXTextSpan textSpan,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol("Discord.UnfurledMediaItemProperties", textSpan, cancellationToken);
        
        public Result<ICSharpTypeSymbol> GetSymbol(
            string name,
            CXTextSpan reference,
            CancellationToken cancellationToken = default
        ) => compilation.GetSymbol<ICSharpTypeSymbol>(name, reference, cancellationToken);
        
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