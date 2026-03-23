namespace ComponentDesigner;

public interface IInterpolationInfo : IEquatable<IInterpolationInfo>, ISourceLocatable
{
    int Id { get; }
    ICSharpTypeSymbol? Symbol { get; }
    Optional<object?> ConstantValue { get; }
}