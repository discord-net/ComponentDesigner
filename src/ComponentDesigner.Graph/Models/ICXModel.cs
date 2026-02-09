namespace ComponentDesigner;

public interface ICXModel : IEquatable<ICXModel>
{
    string Syntax { get; }
    LocationInfo Location { get; }
    int QuoteCount { get; }
    bool UsesDesignerParameter { get; }
    string? DesignerParameterName { get; }
    IReadOnlyList<IInterpolationInfo> Interpolations { get; }
}