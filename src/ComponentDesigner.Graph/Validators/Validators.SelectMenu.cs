using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    private const int SELECT_MENU_PLACEHOLDER_MAX_LENGTH = 150;
    private const int SELECT_MENU_MIN_VALUES_LOWER = 0;
    private const int SELECT_MENU_MAX_VALUES_LOWER = 1;
    private const int SELECT_MENU_MIN_VALUES_UPPER = 25;
    private const int SELECT_MENU_MAX_VALUES_UPPER = 25;

    public static void ValidateSelectMenu(
        IComponentContext context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(selectMenu, state, bag);

        ValidateKindOfSelectMenu();

        // placeholder.length <= 150
        ValueValidators.StringRange(
            context, state.GetPropertyValue(selectMenu.Placeholder), bag,
            upper: SELECT_MENU_PLACEHOLDER_MAX_LENGTH
        );

        // 'minValues' < 'maxValues'
        ValueValidators.PropertyRange(
            context,
            state.GetPropertyValue(selectMenu.MinValues),
            state.GetPropertyValue(selectMenu.MaxValues),
            bag
        );

        // TODO: check for number of options compared to min/max

        // 0 <= 'minValues' <= 25
        ValueValidators.IntRange(
            context, state.GetPropertyValue(selectMenu.MinValues), bag,
            lower: SELECT_MENU_MIN_VALUES_LOWER,
            upper: SELECT_MENU_MIN_VALUES_UPPER
        );

        // 1 <= 'maxValues' <= 25
        ValueValidators.IntRange(
            context, state.GetPropertyValue(selectMenu.MaxValues), bag,
            lower: SELECT_MENU_MAX_VALUES_LOWER,
            upper: SELECT_MENU_MAX_VALUES_UPPER
        );

        ValidateChildren(
            state.Kind is SelectMenuKind.String
                ? IsValidStringSelectMenuChild
                : IsValidEntitySelectMenuChild
        );

        void ValidateKindOfSelectMenu()
        {
            switch (state.Kind)
            {
                case SelectMenuKind.Unknown:
                    bag.Add(
                        Diagnostic.TypelessSelectMenu.At(state.ElementIdentifierTextSpanOrBetter)
                    );
                    break;

                case SelectMenuKind.String:
                    RequireProperty(selectMenu.Options);
                    PropertyNotAllowed(SelectMenuKind.String, state.GetPropertyValue(selectMenu.DefaultValues), bag);
                    PropertyNotAllowed(SelectMenuKind.String, state.GetPropertyValue(selectMenu.ChannelTypes), bag);
                    break;
                
                case SelectMenuKind.Mentionable:
                case SelectMenuKind.Role:
                case SelectMenuKind.User:
                    PropertyNotAllowed(SelectMenuKind.String, state.GetPropertyValue(selectMenu.Options), bag);
                    PropertyNotAllowed(SelectMenuKind.String, state.GetPropertyValue(selectMenu.ChannelTypes), bag);
                    break;
                case SelectMenuKind.Channel:
                    PropertyNotAllowed(SelectMenuKind.String, state.GetPropertyValue(selectMenu.Options), bag);
                    break;
            }
        }

        void ValidateChildren(
            Func<IComponentNode, bool> validator
        )
        {
            foreach (var child in state.Children)
            {
                if (validator(child.Component)) continue;

                bag.Add(
                    Diagnostic
                        .InvalidChildOfComponent(
                            selectMenu,
                            child.Component
                        )
                        .At(child)
                );
            }
        }

        static bool IsValidEntitySelectMenuChild(IComponentNode component)
            => component is IDynamicComponentNode or SelectMenuDefaultValueComponentNode;

        static bool IsValidStringSelectMenuChild(IComponentNode component)
            => component is IDynamicComponentNode or SelectMenuOptionComponentNode;

        static void PropertyNotAllowed(
            SelectMenuKind kind,
            ComponentPropertyValue propertyValue,
            IDiagnosticBag bag
        )
        {
            if (propertyValue.IsSpecified)
            {
                bag.Add(
                    propertyValue.TextSpan.Report(
                        Diagnostic.SelectMenuPropertyNotAllowed(kind, propertyValue.Property)
                    )
                );
            }
        }
        
        void RequireProperty(ComponentProperty property)
            => ValidateProperty(
                selectMenu, state.GetPropertyValue(property), bag,
                isOptional: false,
                requiresValue: true
            );
    }

    public static void ValidateSelectMenuOption(
        IComponentContext context,
        SelectMenuOptionComponentNode option,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(option, state, bag);
    }

    public static void ValidateSelectMenuDefaultValue(
        IComponentContext context,
        SelectMenuDefaultValueComponentNode defaultValue,
        DefaultValueState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(defaultValue, state, bag);
    }
}