using Microsoft.CodeAnalysis;
using RoslynDiagnostic = Microsoft.CodeAnalysis.Diagnostic;
using RoslynDiagnosticDescriptor = Microsoft.CodeAnalysis.DiagnosticDescriptor;
using RoslynDiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;
using ComponentDesignerDiagnostic = ComponentDesigner.Diagnostic;
using ComponentDesignerDiagnosticDescriptor = ComponentDesigner.DiagnosticDescriptor;
using ComponentDesignerDiagnosticSeverity = ComponentDesigner.DiagnosticSeverity;

namespace ComponentDesigner.CSharp;

public static class DiagnosticExtensions
{
    public static RoslynDiagnostic ToRoslyn(
        this ComponentDesignerDiagnostic diagnostic,
        Location location
    ) => RoslynDiagnostic.Create(
        diagnostic.Descriptor.ToRoslyn(),
        location
    );

    public static RoslynDiagnosticDescriptor ToRoslyn(
        this ComponentDesignerDiagnosticDescriptor descriptor)
        => new(
            id: descriptor.Id,
            title: descriptor.Title,
            messageFormat: descriptor.Description ?? descriptor.Title,
            category: "CX",
            defaultSeverity: descriptor.Severity.ToRoslyn(),
            isEnabledByDefault: true
        );

    public static RoslynDiagnosticSeverity ToRoslyn(this ComponentDesignerDiagnosticSeverity severity)
        => (RoslynDiagnosticSeverity)(int)severity;
    
    public static ComponentDesignerDiagnostic ToCX(
        this RoslynDiagnostic diagnostic
    ) => new ComponentDesignerDiagnostic(
        diagnostic.Location.SourceSpan.AsCXTextSpan,
        diagnostic.Descriptor.ToCX()
    );

    public static ComponentDesignerDiagnosticDescriptor ToCX(
        this RoslynDiagnosticDescriptor descriptor)
        => new(
            Id: descriptor.Id,
            Severity: descriptor.DefaultSeverity.ToCX(),
            Title: descriptor.Title.ToString(),
            Description: descriptor.MessageFormat.ToString()
        );

    public static ComponentDesignerDiagnosticSeverity ToCX(this RoslynDiagnosticSeverity severity)
        => (ComponentDesignerDiagnosticSeverity)(int)severity;
}