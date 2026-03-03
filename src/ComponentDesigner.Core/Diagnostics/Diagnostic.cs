using System.Diagnostics;

namespace ComponentDesigner;

public readonly record struct Diagnostic
{
    public CXTextSpan TextSpan { get; init; }
    public DiagnosticDescriptor Descriptor { get; init; }
    
    public string Id => Descriptor.Id;
    public DiagnosticSeverity Severity => Descriptor.Severity;
    public string Title => Descriptor.Title;
    public string? Description => Descriptor.Description;

    public readonly StackTrace StackTrace;
    
    public Diagnostic(
        CXTextSpan textSpan,
        DiagnosticDescriptor descriptor
    )
    {
        TextSpan = textSpan;
        Descriptor = descriptor;

        StackTrace = new();
    }

    public void Deconstruct(out CXTextSpan TextSpan, out DiagnosticDescriptor Descriptor)
    {
        TextSpan = this.TextSpan;
        Descriptor = this.Descriptor;
    }
}