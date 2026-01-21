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
    
    CXDesignerGeneratorState CX { get; }
    
    GeneratorOptions Options { get; }
    
    string GetDesignerValue(int index, string? type = null);

    DesignerInterpolationInfo GetInterpolationInfo(int index);
    DesignerInterpolationInfo GetInterpolationInfo(CXToken token);
    
    ComponentTypingContext RootTypingContext { get; }

    string GetVariableName(string? hint = null);
}

public static class ComponentContextExtensions
{
    extension(IComponentContext context)
    {
        public bool IsModalContext => context.CX.Kind.HasFlag(CXKind.Modal);
        public bool IsMessageContext => context.CX.Kind.HasFlag(CXKind.Message);
    }
    
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
        CXValue.Interpolation interpolation,
        ITypeSymbol? type
    ) => context.GetDesignerValue(interpolation.InterpolationIndex, type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    
    public static string GetDesignerValue(
        this IComponentContext context,
        DesignerInterpolationInfo interpolation,
        string? type = null
    ) => context.GetDesignerValue(interpolation.Id, type);
    
    public static string GetDesignerValue(
        this IComponentContext context,
        DesignerInterpolationInfo interpolation,
        ITypeSymbol? type
    ) => context.GetDesignerValue(interpolation.Id, type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
}