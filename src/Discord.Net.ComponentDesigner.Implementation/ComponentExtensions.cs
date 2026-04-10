using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

public sealed class ComponentExtensions : IComponentExtensionProvider
{
    public static readonly ComponentExtensions Instance = new();

    public ComponentPropertyValueKind? GetPropertyKindOverload(
        IComponentNode component,
        ComponentProperty property
    )
    {
        return null;
    }

    private static readonly ComponentProperty RefProperty = new(
        "ref",
        kind: ComponentPropertyValueKind.Interpolation,
        isOptional: true,
        requiresValue: true,
        isSynthetic: true
    );

    public IReadOnlyList<ComponentProperty> GetAdditionalProperties(IComponentNode component)
    {
        var additional = new List<ComponentProperty>();

        AddRefProperties();

        return additional;

        void AddRefProperties()
        {
            if (
                component
                is ActionRowComponentNode
                or ButtonComponentNode
                or ContainerComponentNode
                or FileComponentNode
                or FileUploadComponentNode
                or LabelComponentNode
                or MediaGalleryComponentNode
                or MediaGalleryItemComponentNode
                or SectionComponentNode
                or SelectMenuComponentNode
                or SeparatorComponentNode
                or TextDisplayComponentNode
                or TextInputComponentNode
                or ThumbnailComponentNode
            )
            {
                additional.Add(RefProperty);
            }
        }
    }
}