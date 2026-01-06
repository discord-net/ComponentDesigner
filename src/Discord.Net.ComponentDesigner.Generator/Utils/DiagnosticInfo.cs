using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    CXTextSpan Span
)
{
    public DiagnosticInfo(DiagnosticDescriptor descriptor, ICXNode node) : this(descriptor, node.Span)
    {
    }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, CXTextSpan span) => new(descriptor, span);
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, ICXNode node) => new(descriptor, node.Span);
}