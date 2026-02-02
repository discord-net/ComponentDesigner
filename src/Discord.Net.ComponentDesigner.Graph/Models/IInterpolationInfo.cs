namespace Discord.CX;

public interface IInterpolationInfo : IEquatable<IInterpolationInfo>
{
    int Id { get; }
    CXTextSpan TextSpan { get; }
    ICSharpTypeSymbol? Symbol { get; }
    Optional<object?> ConstantValue { get; }
}