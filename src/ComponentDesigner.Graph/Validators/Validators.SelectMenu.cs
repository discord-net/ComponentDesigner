using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int SELECT_MENU_PLACEHOLDER_MAX_LENGTH = 150;
    public const int SELECT_MENU_MIN_VALUES_LOWER = 0;
    public const int SELECT_MENU_MAX_VALUES_LOWER = 1;
    public const int SELECT_MENU_MIN_VALUES_UPPER = 25;
    public const int SELECT_MENU_MAX_VALUES_UPPER = 25;
    public const int SELECT_MENU_MIN_OPTIONS = 1;
    public const int SELECT_MENU_MAX_OPTIONS = 25;
    
    public const int SELECT_MENU_OPTION_VALUE_MAX_LENGTH = 100;
    public const int SELECT_MENU_OPTION_LABEL_MAX_LENGTH = 100;
    public const int SELECT_MENU_OPTION_DESCRIPTION_MAX_LENGTH = 100;

    public static void ValidateSelectMenu(
        IComponentContext context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken
    )
    {
        ValidateGenericComponent(context, selectMenu, state, bag);

        ValidateKindOfSelectMenu();
        ReportDisabledInModal();

        // placeholder.length <= 150
        StringNotEmptyAndRange(
            context, state.GetPropertyValue(selectMenu.Placeholder), bag,
            upper: SELECT_MENU_PLACEHOLDER_MAX_LENGTH
        );

        Analysis.TryCreateRangeFromProperties(
            state.GetPropertyValue(selectMenu.MinValues),
            state.GetPropertyValue(selectMenu.MaxValues),
            out var minMaxRange
        );

        if (minMaxRange.IsBoundedRange && minMaxRange.Lower > minMaxRange.Upper)
        {
            bag.Add(
                Diagnostic
                    .OutOfRange(
                        selectMenu.MinValues,
                        selectMenu.MaxValues,
                        minMaxRange.Lower.Value,
                        minMaxRange.Upper.Value
                    )
                    .At(state.GetPropertyValue(selectMenu.MinValues))
            );
        }

        if (minMaxRange.Lower is < SELECT_MENU_MIN_VALUES_LOWER or > SELECT_MENU_MIN_VALUES_UPPER)
        {
            bag.Add(
                Diagnostic
                    .IntegerOutOfRange(
                        selectMenu.MinValues,
                        minMaxRange.Lower.Value,
                        lower: SELECT_MENU_MIN_VALUES_LOWER,
                        upper: SELECT_MENU_MIN_VALUES_UPPER
                    )
                    .At(state.GetPropertyValue(selectMenu.MinValues))
            );
        }

        if (minMaxRange.Upper is < SELECT_MENU_MAX_VALUES_LOWER or > SELECT_MENU_MAX_VALUES_UPPER)
        {
            bag.Add(
                Diagnostic
                    .IntegerOutOfRange(
                        selectMenu.MaxValues,
                        minMaxRange.Upper.Value,
                        lower: SELECT_MENU_MAX_VALUES_LOWER,
                        upper: SELECT_MENU_MAX_VALUES_UPPER
                    )
                    .At(state.GetPropertyValue(selectMenu.MaxValues))
            );
        }

        ValidateStringSelectOptions();
        ValidateEntitySelectDefaultValues();

        void ReportDisabledInModal()
        {
            if (context.Options.Target is not ComponentTargetType.Modal)
                return;
            
            var disabledPropertyValue = state.GetPropertyValue(selectMenu.Disabled);

            if (disabledPropertyValue.IsSpecified)
            {
                bag.Add(
                    Diagnostic
                        .PropertyNotAllowedForTarget(disabledPropertyValue.Name, context.Options.Target)
                        .At(disabledPropertyValue)
                );
            }
        }
        
        void ValidateStringSelectOptions()
        {
            if (state.Kind is not SelectMenuKind.String) return;

            var options = state.GetPropertyValue(selectMenu.Options);

            if (
                !context.Implementation.TryAnalyzeNumberOfValues(
                    context,
                    selectMenu,
                    options,
                    cancellationToken,
                    out var range
                )
            )
            {
                range = options.AsFlattened.OfType<ComponentPropertyValue.Component>().Count();
            }

            var lower = minMaxRange.Lower ?? SELECT_MENU_MIN_OPTIONS;

            if (range.Upper > SELECT_MENU_MAX_OPTIONS)
            {
                bag.Add(
                    Diagnostic
                        .IntegerOutOfRange(
                            selectMenu.Options,
                            range.Upper.Value,
                            lower: lower,
                            upper: SELECT_MENU_MAX_OPTIONS
                        )
                        .At(options)
                );
            }

            foreach (var component in options.AsFlattened.OfType<ComponentPropertyValue.Component>())
            {
                if (!IsValidStringSelectMenuChild(component.GraphNode.Component))
                    bag.Add(
                        Diagnostic
                            .InvalidChildOfComponent(
                                selectMenu,
                                component.GraphNode.Component
                            )
                            .At(component.GraphNode.State)
                    );
            }
        }

        void ValidateEntitySelectDefaultValues()
        {
            if (state.Kind is SelectMenuKind.Unknown or SelectMenuKind.String) return;

            var defaultValues = state.GetPropertyValue(selectMenu.DefaultValues);

            if (
                !context.Implementation.TryAnalyzeNumberOfValues(
                    context,
                    selectMenu,
                    defaultValues,
                    cancellationToken,
                    out var range
                )
            )
            {
                range = defaultValues.AsFlattened.OfType<ComponentPropertyValue.Component>().Count();
            }

            if (!minMaxRange.Contains(range))
            {
                bag.Add(
                    Diagnostic
                        .OutOfRange(
                            selectMenu.DefaultValues,
                            minMaxRange,
                            range
                        )
                        .At(defaultValues)
                );
            }
            
            foreach (var component in defaultValues.AsFlattened.OfType<ComponentPropertyValue.Component>())
            {
                if (!IsValidEntitySelectMenuChild(component.GraphNode.Component))
                    bag.Add(
                        Diagnostic
                            .InvalidChildOfComponent(
                                selectMenu,
                                component.GraphNode.Component
                            )
                            .At(component.GraphNode.State)
                    );
            }
        }

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
            if (propertyValue.IsSome)
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
                isOptionalOverload: false,
                requiresValueOverload: true
            );
    }

    public static void ValidateSelectMenuOption(
        IComponentContext context,
        SelectMenuOptionComponentNode option,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, option, state, bag, isParentOfOtherComponents: false);
        
        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(option.Value),
            bag,
            upper: SELECT_MENU_OPTION_VALUE_MAX_LENGTH
        );

        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(option.Label),
            bag,
            upper: SELECT_MENU_OPTION_LABEL_MAX_LENGTH
        );
        
        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(option.Description),
            bag,
            upper: SELECT_MENU_OPTION_DESCRIPTION_MAX_LENGTH
        );
    }

    public static void ValidateSelectMenuDefaultValue(
        IComponentContext context,
        SelectMenuDefaultValueComponentNode defaultValue,
        DefaultValueState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, defaultValue, state, bag);
    }
}