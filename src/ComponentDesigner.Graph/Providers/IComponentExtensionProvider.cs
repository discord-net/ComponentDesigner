using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IComponentExtensionProvider
{
    ComponentPropertyValueKind? GetPropertyKindOverload(
        IComponentNode component,
        ComponentProperty property
    );

    IReadOnlyList<ComponentProperty> GetAdditionalProperties(
        IComponentNode component
    );
}