using System;
using System.Collections.Generic;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes;

public interface IComponentContext
{
    KnownTypes KnownTypes { get; }
    Compilation Compilation { get; }

    bool HasErrors { get; }

    IReadOnlyList<Diagnostic> GlobalDiagnostics { get; }

    void AddDiagnostic(Diagnostic diagnostic);

    Location GetLocation(TextSpan span);

    IDisposable CreateDiagnosticScope(List<Diagnostic> bag);

    string GetDesignerValue(int index, string? type = null);

    DesignerInterpolationInfo GetInterpolationInfo(int index);
    DesignerInterpolationInfo GetInterpolationInfo(CXToken token);
    
    ComponentTypingContext RootTypingContext { get; }
}

public static class ComponentContextExtensions
{
    public static DesignerInterpolationInfo GetInterpolationInfo(
        this IComponentContext context,
        CXValue.Interpolation interpolation
    ) => context.GetInterpolationInfo(interpolation.InterpolationIndex);

    public static string GetDesignerValue(
        this IComponentContext context,
        CXValue.Interpolation interpolation,
        string? type = null
    ) => context.GetDesignerValue(interpolation.InterpolationIndex, type);

    public static string GetDesignerValue(
        this IComponentContext context,
        DesignerInterpolationInfo interpolation,
        string? type = null
    ) => context.GetDesignerValue(interpolation.Id, type);

    public static Location GetLocation(this IComponentContext context, ICXNode node)
        => context.GetLocation(node.Span);

    public static void AddDiagnostic(
        this IComponentContext context,
        DiagnosticDescriptor descriptor,
        ICXNode node,
        params object?[]? args
    ) => context.AddDiagnostic(
        Diagnostic.Create(
            descriptor,
            GetLocation(context, node),
            args
        )
    );

    public static void AddDiagnostic(
        this IComponentContext context,
        DiagnosticDescriptor descriptor,
        TextSpan span,
        params object?[]? args
    ) => context.AddDiagnostic(
        Diagnostic.Create(
            descriptor,
            context.GetLocation(span),
            args
        )
    );
}