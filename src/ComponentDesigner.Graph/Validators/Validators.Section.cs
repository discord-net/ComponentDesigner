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
        ValidateGenericComponent(context, section, state, bag);

        ValidateChildComponents();
        ValidateAccessory();

        void ValidateAccessory()
        {
            var accessory = state.GetPropertyValue(section.Accessory);

            if (accessory.IsNone)
            {
                bag.Add(
                    Diagnostic
                        .RequiredPropertyNotSpecified(section, section.Accessory)
                        .At(state.ElementIdentifierTextSpanOrBetter)
                );

                return;
            }

            if (accessory is not ComponentPropertyValue.Component { GraphNode.Component: var accessoryComponent })
            {
                bag.Add(
                    Diagnostic
                        .InvalidPropertyValue(accessory)
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

            if (components.IsNone) return;

            if (components is not ComponentPropertyValue.Many many)
            {
                bag.Add(
                    Diagnostic
                        .InvalidPropertyValue(components)
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
                if (childComponentValue is not ComponentPropertyValue.Component{GraphNode.Component: var childComponent})
                {
                    bag.Add(
                        Diagnostic
                            .InvalidPropertyValue(childComponentValue)
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