using System;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed class DesignerInterpolationInfo(
    int id,
    TextSpan span,
    ITypeSymbol? symbol,
    Optional<object?> constant
) : IEquatable<DesignerInterpolationInfo>
{
    public int Id { get; } = id;
    public TextSpan Span { get; } = span;
    public ITypeSymbol? Symbol { get; } = symbol;
    public Optional<object?> Constant { get; } = constant;
    
    public bool Equals(DesignerInterpolationInfo? other)
    {
        if (other is null) return false;
        
        if (ReferenceEquals(this, other)) return true;
        
        return Id == other.Id &&
               Span.Equals(other.Span) &&
               Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
               other.Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) &&
               (
                   (Constant.HasValue, other.Constant.HasValue) switch
                   {
                       (false, false) => true,
                       (false, true) or (true, false) => false,
                       (true, true) => (Constant.Value?.Equals(other.Constant.Value) ?? other.Constant.Value is null)
                   }
               );
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is DesignerInterpolationInfo other && Equals(other);
    }

    public override int GetHashCode()
        => Hash.Combine(
            Id,
            Span,
            Constant,
            Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
}