using System.Diagnostics;
using System.Text;
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

        StackTrace = new(skipFrames: 1);
    }

    public bool Equals(Diagnostic other)
        => TextSpan == other.TextSpan &&
           Descriptor == other.Descriptor;

    public override int GetHashCode()
        => Hash.Combine(TextSpan, Descriptor);

    public override string ToString()
    {
        using (StringBuilder.Pooled(out var sb))
        {
            sb.Append(TextSpan).Append(' ');
            
            sb.Append('[').Append(Id).Append(" | ").Append(Severity).Append("] ");
            sb.Append(Title);

            if (Description is not null)
                sb.Append(": ").Append(Description);

            return sb.ToString();
        }
    }
}