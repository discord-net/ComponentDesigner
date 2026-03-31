namespace ComponentDesigner;

public interface IInterpolationInfo :
    ISourceLocatable
{
    int Id { get; }
    ICSharpTypeSymbol? Symbol { get; }
    Optional<object?> ConstantValue { get; }
}