using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateTextDisplay(
        IComponentContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        IDiagnosticBag bag
    )
    {
        ValidateElementStructure(textDisplay, state, bag);
        ValidateProperty(textDisplay, state.GetPropertyValue(textDisplay.Id), bag);
        ReportDiagnosticsForUnknownProperties(textDisplay, state, bag);

        ValidateContent();

        void ValidateContent()
        {
            var content = state.GetPropertyValue(textDisplay.Content);

            if (!content.HasValue)
            {
                bag.Add(
                    Diagnostic
                        .RequiredPropertyNotSpecified(textDisplay, textDisplay.Content)
                        .At(state.ElementIdentifierTextSpanOrBetter)
                );

                return;
            }

            switch (content)
            {
                case ComponentPropertyValue.AttributeValue:
                    // OK
                    break;
                case ComponentPropertyValue.Component { GraphNode.Component: { } component }:
                    if (component is not TextControlNode)
                    {
                        bag.Add(
                            Diagnostic
                                .InvalidChildOfComponent(textDisplay, component)
                                .At(content)
                        );
                    }

                    break;
                default:
                    bag.Add(
                        Diagnostic
                            .InvalidPropertyValue(
                                content,
                                "string",
                                "text controls"
                            )
                            .At(content)
                    );
                    break;
            }
        }
    }
}