using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner.LanguageServer;

public static class ComponentValidityMap
{
    private static readonly Dictionary<Type, HashSet<Type>> ValidityMap = new()
    {
        {
            typeof(ActionRowComponentNode),
            [
                typeof(ButtonComponentNode),
                typeof(SelectMenuComponentNode)
            ]
        },
        {
            typeof(SectionComponentNode),
            [
                typeof(TextDisplayComponentNode)
            ]
        },
        {
            typeof(MediaGalleryComponentNode),
            [
                typeof(MediaGalleryItemComponentNode)
            ]
        },
        {
            typeof(ContainerComponentNode),
            [
                typeof(ActionRowComponentNode),
                typeof(TextDisplayComponentNode),
                typeof(SectionComponentNode),
                typeof(MediaGalleryComponentNode),
                typeof(SeparatorComponentNode),
                typeof(FileComponentNode)
            ]
        },
        {
            typeof(LabelComponentNode),
            [
                typeof(TextInputComponentNode),
                typeof(SelectMenuComponentNode)
            ]
        }
    };

    private static readonly HashSet<Type> TopLevelComponents =
    [
        typeof(ActionRowComponentNode),
        typeof(SectionComponentNode),
        typeof(TextDisplayComponentNode),
        typeof(MediaGalleryComponentNode),
        typeof(FileComponentNode),
        typeof(ContainerComponentNode),
        typeof(SeparatorComponentNode)
    ];

    public static bool IsValidHierarchy(
        GraphNode? parent,
        IComponentNode child
    )
    {
        
        if (parent is null)
            return child is IDynamicComponentNode || TopLevelComponents.Contains(child.GetType());

        var parentType = parent.Component.GetType();
        var childType = child.GetType();
        
        // special case for select menu
        if (
            parent is
            {
                Component: SelectMenuComponentNode,
                State: SelectMenuState selectMenuState
            }
        )
        {
            return selectMenuState.Kind switch
            {
                SelectMenuKind.String => child is IDynamicComponentNode or SelectMenuOptionComponentNode,
                _ => child is IDynamicComponentNode or SelectMenuDefaultValueComponentNode
            };
        }
        
        if (!ValidityMap.TryGetValue(parentType, out var mapping))
            return false;

        return child is IDynamicComponentNode || mapping.Contains(childType);
    }
}