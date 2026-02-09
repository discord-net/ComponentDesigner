namespace ComponentDesigner;

public readonly record struct Diagnostic(
    CXTextSpan TextSpan,
    DiagnosticDescriptor Descriptor
)
{
    public string Id => Descriptor.Id;
    public DiagnosticSeverity Severity => Descriptor.Severity;
    public string Title => Descriptor.Title;
    public string? Description => Descriptor.Description;
}