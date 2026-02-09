namespace ComponentDesigner.CSharp;

public static class OptionalExtensions
{
    extension<T>(Microsoft.CodeAnalysis.Optional<T> opt)
    {
        public Optional<T> AsComponentDesignerOptional => opt.HasValue
            ? new Optional<T>(opt.Value)
            : default;
    }
}