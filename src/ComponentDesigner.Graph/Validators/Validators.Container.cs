using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateContainer(
        IComponentContext context,
        ContainerComponentNode container,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(container, state, bag);
        
        PropertyMatchesComponents(
            container, state.GetPropertyValue(container.Components), bag,
            static x => x
                is IDynamicComponentNode
                or ActionRowComponentNode
                or TextDisplayComponentNode
                or SectionComponentNode
                or MediaGalleryComponentNode
                or SeparatorComponentNode
                or FileComponentNode
        );
    }
    
}