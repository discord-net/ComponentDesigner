using System;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class InterpolationInfo(
    int id,
    CXTextSpan textSpan,
    ICSharpTypeSymbol? symbol,
    Optional<object?> constantValue
) : IInterpolationInfo, IEquatable<InterpolationInfo>
{
    public int Id { get; } = id;

    public CXTextSpan TextSpan { get; } = textSpan;

    public ICSharpTypeSymbol? Symbol { get; } = symbol;

    public Optional<object?> ConstantValue { get; } = constantValue;

    public bool Equals(InterpolationInfo? other)
        => other is not null &&
           Id == other.Id &&
           TextSpan == other.TextSpan &&
           (Symbol?.Equals(other.Symbol!) ?? other.Symbol is null) &&
           ConstantValue == other.ConstantValue;

    public override bool Equals(object? obj)
        => obj is InterpolationInfo other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Id, TextSpan, Symbol, ConstantValue);

    bool IEquatable<IInterpolationInfo>.Equals(IInterpolationInfo other)
        => other is InterpolationInfo info && Equals(info);
}