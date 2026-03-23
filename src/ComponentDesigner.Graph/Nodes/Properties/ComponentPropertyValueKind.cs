namespace ComponentDesigner.Nodes;

[Flags]
public enum ComponentPropertyValueKind
{
    None = 0,

    Literal = 1 << 0,
    Interpolation = 1 << 1,

    Component = 1 << 2,

    Many = 1 << 3,

    SyntaxValue = Many | Literal | Interpolation,
    ManyComponents = Many | Component,
    SingleSyntaxValue = Literal | Interpolation,

    Any = SyntaxValue | ManyComponents,
}

public static class ComponentPropertyValueKindExtensions
{
    extension(ComponentPropertyValueKind kind)
    {
        public string ReadableName => kind switch
        {
            ComponentPropertyValueKind.None => "None",
            ComponentPropertyValueKind.Literal => "Literal",
            ComponentPropertyValueKind.Interpolation => "Interpolation",
            ComponentPropertyValueKind.Component => "Component",
            ComponentPropertyValueKind.SyntaxValue => "Syntax Value",
            ComponentPropertyValueKind.ManyComponents => "One or more Components",
            ComponentPropertyValueKind.SingleSyntaxValue => "Literal or Interpolation",
            ComponentPropertyValueKind.Any => "Any",
            _ => string.Join(
                " or ", ((ComponentPropertyValueKind[])Enum.GetValues(typeof(ComponentPropertyValueKind)))
                .Where(x => kind.HasFlag(x))
                .Select(x => x.ReadableName)
            )
        };
    }
}