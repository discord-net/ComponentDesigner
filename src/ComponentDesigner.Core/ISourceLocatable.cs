namespace ComponentDesigner;

public interface ISourceLocatable
{
    /// <summary>
    ///     Gets the span representing this object in source.
    /// </summary>
    CXTextSpan TextSpan { get; }
}