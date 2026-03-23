using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetComponentDesignerImplementation
{
    public bool TryAnalyzeNumberOfValues(
        IComponentContext context,
        IComponentNode component,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken,
        out StaticRange range
    )
    {
        range = StaticRange.Empty;
        
        switch (component)
        {
            case MediaGalleryComponentNode mediaGallery when propertyValue.Property == mediaGallery.Items:
                AnalyzeMediaGalleryItems(ref range);
                return true;
        }

        return false;
        
        void AnalyzeMediaGalleryItems(ref StaticRange range)
        {
            foreach (var value in propertyValue.AsFlattened)
            {
                switch (value)
                {
                    case ComponentPropertyValue.Component:
                        range++;
                        break;
                    
                    case ComponentPropertyValue.Interpolation {Info: var info}:
                        if (info.Symbol.TryGetEnumerableType(out _))
                        {
                            range = range.WithBoundedLower();
                            return;
                        }
                        
                        // at least one value
                        range++;
                        break;
                }
            }
            
            
        }
    }
}