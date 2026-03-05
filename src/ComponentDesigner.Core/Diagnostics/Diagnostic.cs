using System.Diagnostics;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public readonly struct Diagnostic : IEquatable<Diagnostic>
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

    public bool Equals(Diagnostic other)
        => TextSpan == other.TextSpan &&
           Descriptor == other.Descriptor;

    public override int GetHashCode()
        => Hash.Combine(TextSpan, Descriptor);
}