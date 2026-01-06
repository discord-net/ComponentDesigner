using System.Collections.Generic;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public interface IDiagnosticBag
{
    void AddDiagnostic(DiagnosticInfo info);
}

public static class DiagnosticBagExtensions
{
    extension<T>(T bag) where T : IDiagnosticBag
    {
        public void AddDiagnostic(DiagnosticDescriptor descriptor, CXTextSpan span) => bag.AddDiagnostic(new DiagnosticInfo(descriptor, span));
        public void AddDiagnostic(DiagnosticDescriptor descriptor, ICXNode node) => bag.AddDiagnostic(new DiagnosticInfo(descriptor, node.Span));
    }

    extension(IList<DiagnosticInfo> bag)
    {
        public void Add(DiagnosticDescriptor descriptor, CXTextSpan span) => bag.Add(new DiagnosticInfo(descriptor, span));
        public void Add(DiagnosticDescriptor descriptor, ICXNode node) => bag.Add(new DiagnosticInfo(descriptor, node.Span));
    }
}