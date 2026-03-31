using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner.LanguageServer;

public enum AutoFillKind
{
    None,
    Component,
    Interpolation,
    String,
    Choices
}

public sealed record PropertyCompletionInfo(
    IComponentNode Component,
    ComponentProperty Property,
    string AutoFill,
    AutoFillKind AutoFillKind,
    ComponentState? State = null
)
{
    public bool HasAutoFill => !string.IsNullOrWhiteSpace(AutoFill);
    
    public string? Description { get; } = Documentation.GetDescriptionOfProperty(Component, Property, State);

    public string Details { get; } = (Property.IsOptional, Property.RequiresValue) switch
    {
        (false, false) => "(required flag)",
        (false, true) => "(required)",
        (true, false) => "(optional flag)",
        (true, true) => "(optional)"
    };

    public static PropertyCompletionInfo Get(
        IComponentNode component,
        ComponentProperty property,
        ComponentState? state = null
    )
    {
        var autoFill = string.Empty;

        if (!TryGetAutoFill(ref autoFill, out var autoFillKind))
            autoFill = GetDefaultAutoFill(out autoFillKind);

        return new PropertyCompletionInfo(
            component, 
            property, 
            autoFill,
            autoFillKind,
            state
        );
        
        bool TryGetAutoFill(ref string autoFill, out AutoFillKind kind)
        {
            switch (component)
            {
                case ButtonComponentNode button when property == button.Style:
                    autoFill = Choices(ButtonComponentNode.ValidButtonStyles);
                    kind = AutoFillKind.Choices;
                    return true;
                
                case SeparatorComponentNode separator when property == separator.Spacing:
                    autoFill = Choices("large", "small");
                    kind = AutoFillKind.Choices;
                    return true;
                
                case TextInputComponentNode textInput when property == textInput.Style:
                    autoFill = Choices("short", "paragraph");
                    kind = AutoFillKind.Choices;
                    return true;
                
                case ButtonComponentNode button when property == button.Disabled:
                case ContainerComponentNode container when property == container.IsSpoiler:
                case FileComponentNode file when property == file.IsSpoiler:
                case FileUploadComponentNode fileUpload when property == fileUpload.Required:
                case SeparatorComponentNode separator when property == separator.Divider:
                case TextInputComponentNode textInput when property == textInput.Required:
                case ThumbnailComponentNode thumbnail when property == thumbnail.IsSpoiler:
                    autoFill = Choices("true", "false");
                    kind = AutoFillKind.Choices;
                    return true;
            }

            kind = AutoFillKind.None;
            return false;
        }

        string GetDefaultAutoFill(out AutoFillKind kind)
        {
            if (property.Kind.HasFlag(ComponentPropertyValueKind.Literal))
            {
                kind = AutoFillKind.String;
                return "='$0'";
            }

            if (property.Kind.HasFlag(ComponentPropertyValueKind.Interpolation))
            {
                kind = AutoFillKind.Interpolation;
                return @"=\{$0\}";
            }

            if (property.Kind.HasFlag(ComponentPropertyValueKind.Component))
            {
                kind = AutoFillKind.Component;
                return "=($0)";
            }

            kind = AutoFillKind.None;
            return string.Empty;
        }
        
        static string Choices(params IEnumerable<string> choices)
            => $"=\'${{1|{string.Join(",", choices)}|}}\'";
    }
}