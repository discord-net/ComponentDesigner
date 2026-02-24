using System;
using System.Collections.Generic;
using System.Linq;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class CXModel(
    string syntax,
    LocationInfo location,
    int quoteCount,
    bool usesDesignerParameter,
    string? designerParameterName,
    string? designerParameterType,
    IReadOnlyList<InterpolationInfo> interpolations
) : ICXModel, IEquatable<CXModel>
{
    public string Syntax { get; } = syntax;

    public LocationInfo Location { get; } = location;

    public int QuoteCount { get; } = quoteCount;

    public bool UsesDesignerParameter { get; } = usesDesignerParameter;

    public string? DesignerParameterName { get; } = designerParameterName;
    public string? DesignerParameterType { get; } = designerParameterType;

    public IReadOnlyList<InterpolationInfo> Interpolations { get; } = interpolations;

    public bool Equals(CXModel? other)
        => other is not null &&
           other.Syntax == Syntax &&
           other.Location == Location &&
           other.QuoteCount == QuoteCount &&
           other.UsesDesignerParameter == UsesDesignerParameter &&
           other.DesignerParameterType == DesignerParameterType && 
           other.DesignerParameterName == DesignerParameterName &&
           Interpolations.SequenceEqual(other.Interpolations);

    public override bool Equals(object? obj)
        => obj is CXModel other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Syntax, Location, QuoteCount, UsesDesignerParameter, DesignerParameterName,
            DesignerParameterType, Interpolations.Aggregate(0, Hash.Combine)
        );

    IReadOnlyList<IInterpolationInfo> ICXModel.Interpolations => Interpolations;
    bool IEquatable<ICXModel>.Equals(ICXModel other) => other is CXModel cx && Equals(cx);
}