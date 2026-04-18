using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int BUTTON_LABEL_MAX_LENGTH = 80;
    public const int BUTTON_URL_MAX_LENGTH = 512;
    public const int BUTTON_CUSTOM_ID_MIN_LENGTH = 1;
    public const int BUTTON_CUSTOM_ID_MAX_LENGTH = 100;

    public static void ValidateButton(
        IComponentContext context,
        ButtonComponentNode button,
        ButtonState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, button, state, bag, isParentOfOtherComponents: false);

        StringRange(
            context, state.GetPropertyValue(button.Label), bag,
            upper: BUTTON_LABEL_MAX_LENGTH
        );
        StringRange(
            context, state.GetPropertyValue(button.CustomId), bag,
            lower: BUTTON_CUSTOM_ID_MIN_LENGTH,
            upper: BUTTON_CUSTOM_ID_MAX_LENGTH
        );
        StringRange(
            context, state.GetPropertyValue(button.Url), bag,
            upper: BUTTON_URL_MAX_LENGTH
        );

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
                state.InferredKind ?? ButtonKind.Default,
                state.GetPropertyValue(property),
                bag
            );

        void RequireProperty(ComponentProperty property)
            => ValidateProperty(
                button, state.GetPropertyValue(property), bag,
                isOptionalOverload: false,
                requiresValueOverload: true
            );

        // void LabelIsNotDuplicatedInChildrenOfElement()
        // {
        //     var label = state.GetPropertyValue(button.Label);
        //
        //     if (label.HasAttribute && state.CXNode.Children.Count > 0)
        //     {
        //         bag.Add(
        //             state.CXNode.Children.Report(
        //                 Diagnostic.ChildSuppliedExclusivePropertyDuplicated(
        //                     label.UsedName
        //                 )
        //             )
        //         );
        //     }
        // }

        static void PropertyNotAllowed(ButtonKind kind, ComponentPropertyValue propertyValue, IDiagnosticBag bag)
        {
            if (propertyValue.IsSome)
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