namespace ComponentDesigner;

public interface ICXModel 
{
    LocationInfo Location { get; }
    string Syntax { get; }
    int QuoteCount { get; }
    bool UsesDesignerParameter { get; }
    string? DesignerParameterName { get; }
    IReadOnlyList<IInterpolationInfo> Interpolations { get; }
}