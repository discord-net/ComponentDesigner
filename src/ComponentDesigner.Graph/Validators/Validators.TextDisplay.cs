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
        ValidateProperty(textDisplay, state.GetPropertyValue(textDisplay.Id), bag);
        ReportDiagnosticsForUnknownProperties(textDisplay, state, bag);

        // content can be either the property or in the state
        var contentProperty = state.GetPropertyValue(textDisplay.Content);
        
        // the property is exclusive with the states text control
        if (contentProperty.IsSpecified && state.Content is not null)
        {
            bag.Add(
                contentProperty.TextSpan.Report(
                    Diagnostic.ChildSuppliedExclusivePropertyDuplicated(contentProperty.UsedName)
                )
            );
            
            return;
        }
        
        if (!contentProperty.HasValue && state.Content is null)
        {
            bag.Add(
                state.ElementIdentifierTextSpanOrBetter.Report(
                    Diagnostic.RequiredPropertyNotSpecified(textDisplay, textDisplay.Content)
                )
            );
            return;
        }
    }
}