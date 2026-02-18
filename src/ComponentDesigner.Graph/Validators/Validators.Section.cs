using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int SECTION_MAX_COMPONENTS = 3;

    public static void ValidateSection(
        IComponentContext context,
        SectionComponentNode section,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateElementStructure(section, state, bag);
        ValidateProperty(section, state.GetPropertyValue(section.Id), bag);
        ReportDiagnosticsForUnknownProperties(section, state, bag);

        ValidateChildComponents();
        ValidateAccessory();

        void ValidateAccessory()
        {
            var accessory = state.GetPropertyValue(section.Accessory);

            if (!accessory.HasValue)
            {
                bag.Add(
                    Diagnostic
                        .RequiredPropertyNotSpecified(section, section.Accessory)
                        .At(state.ElementIdentifierTextSpanOrBetter)
                );

                return;
            }

            if (accessory.GraphNode?.Component is not {} accessoryComponent)
            {
                bag.Add(
                    Diagnostic
                        .InvalidPropertyValue(
                            accessory,
                            ComponentPropertyValueKind.Component
                        )
                        .At(accessory)
                );

                return;
            }

            if (!IsValidAccessory(accessoryComponent))
            {
                bag.Add(
                    Diagnostic
                        .InvalidAccessoryComponentOfSection(accessoryComponent)
                        .At(accessory)
                );
            }
        }


        void ValidateChildComponents()
        {
            var components = state.GetPropertyValue(section.Components);

            if (!components.HasValue)
            {
                bag.Add(
                    Diagnostic
                        .ComponentRequiresAtLeastOneChild(section)
                        .At(state.TextSpan)
                );
                return;
            }

            if (components is not ComponentPropertyValue.Many many)
            {
                bag.Add(
                    Diagnostic
                        .InvalidPropertyValue(
                            components,
                            "components"
                        )
                        .At(components)
                );

                return;
            }

            if (many.Values.Count > SECTION_MAX_COMPONENTS)
            {
                bag.Add(
                    Diagnostic
                        .TooManyChildren(
                            section,
                            SECTION_MAX_COMPONENTS
                        )
                        .At(
                            CXTextSpan.From(
                                many.Values,
                                start: SECTION_MAX_COMPONENTS
                            )
                        )
                );

                return;
            }

            foreach (var childComponentValue in many.Values)
            {
                if (childComponentValue.GraphNode?.Component is not {} childComponent)
                {
                    bag.Add(
                        Diagnostic
                            .InvalidPropertyValue(
                                childComponentValue,
                                ComponentPropertyValueKind.Component
                            )
                            .At(childComponentValue)
                    );

                    continue;
                }

                if (!IsValidComponentOfSection(childComponent))
                {
                    bag.Add(
                        Diagnostic
                            .InvalidChildComponentOfSection(childComponent)
                            .At(childComponentValue)
                    );
                }
            }
        }

        static bool IsValidAccessory(IComponentNode node)
            => node is IDynamicComponentNode
                or ButtonComponentNode
                or ThumbnailComponentNode;

        static bool IsValidComponentOfSection(IComponentNode node)
            => node is IDynamicComponentNode
                or TextDisplayComponentNode;
    }
}