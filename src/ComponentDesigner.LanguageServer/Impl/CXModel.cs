using ComponentDesigner;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed record CXModel(
    string Syntax,
    LocationInfo Location,
    int QuoteCount,
    bool UsesDesignerParameter,
    string? DesignerParameterName,
    IReadOnlyList<IInterpolationInfo> Interpolations
) : ICXModel
{
    public bool Equals(ICXModel? obj)
        => obj is CXModel other && Equals(other);
}