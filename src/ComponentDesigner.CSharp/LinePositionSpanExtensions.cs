using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public static class LinePositionSpanExtensions
{
    extension(LinePositionSpan)
    {
        public static LinePositionSpan From(FileLinePositionSpan roslyn)
            => new(new(roslyn.StartLinePosition.Line, roslyn.StartLinePosition.Character),
                new(roslyn.EndLinePosition.Line, roslyn.EndLinePosition.Character));
    }
}