using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateButton(
        IComponentContext context,
        ButtonComponentNode button,
        ButtonState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(button, state, bag);

        LabelIsNotDuplicatedInChildrenOfElement();

        switch (state.InferredKind)
        {
            case ButtonKind.Link:
                RequireProperty(button.Url);
                DisallowProperty(button.CustomId);
                DisallowProperty(button.SkuId);
                RequireOneOf(
                    button, state, bag,
                    state.GetPropertyValue(button.Label),
                    state.GetPropertyValue(button.Emoji)
                );
                break;

            case ButtonKind.Premium:
                RequireProperty(button.SkuId);
                DisallowProperty(button.CustomId);
                DisallowProperty(button.Url);
                DisallowProperty(button.Label);
                DisallowProperty(button.Emoji);
                break;

            case ButtonKind.Default:
                RequireProperty(button.CustomId);
                DisallowProperty(button.SkuId);
                DisallowProperty(button.Url);
                RequireOneOf(
                    button, state, bag,
                    state.GetPropertyValue(button.Label), state.GetPropertyValue(button.Emoji)
                );
                break;
        }

        void DisallowProperty(ComponentProperty property)
            => PropertyNotAllowed(
                ButtonKind.Link,
                state.GetPropertyValue(property),
                bag
            );

        void RequireProperty(ComponentProperty property)
            => ValidateProperty(
                button, state.GetPropertyValue(property), bag,
                isOptional: false,
                requiresValue: true
            );

        void LabelIsNotDuplicatedInChildrenOfElement()
        {
            var label = state.GetPropertyValue(button.Label);

            if (label.HasAttribute && state.CXNode.Children.Count > 0)
            {
                bag.Add(
                    state.CXNode.Children.Report(
                        Diagnostic.ChildSuppliedExclusivePropertyDuplicated(
                            label.UsedName
                        )
                    )
                );
            }
        }

        static void PropertyNotAllowed(ButtonKind kind, ComponentPropertyValue propertyValue, IDiagnosticBag bag)
        {
            if (propertyValue.IsSpecified)
            {
                bag.Add(
                    propertyValue.TextSpan.Report(
                        Diagnostic.ButtonPropertyNotAllowed(kind, propertyValue.Property)
                    )
                );
            }
        }
    }
}