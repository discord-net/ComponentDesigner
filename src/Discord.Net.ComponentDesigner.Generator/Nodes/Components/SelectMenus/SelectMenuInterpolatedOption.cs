using Discord.CX.Parser;
using Discord.Net.ComponentDesignerGenerator.Utils;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components.SelectMenus;

public sealed class SelectMenuInterpolatedOption
{
    public CXToken Interpolation { get; }
    public int InterpolationId { get; }
    public bool IsCollection { get; }
    public bool IsBuilder { get; }

    private SelectMenuInterpolatedOption(
        CXToken interpolation,
        int interpolationId,
        bool isCollection,
        bool isBuilder
    )
    {
        Interpolation = interpolation;
        InterpolationId = interpolationId;
        IsCollection = isCollection;
        IsBuilder = isBuilder;
    }

    public string Render(
        SelectMenuComponentNode.SelectState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        var source = context.GetDesignerValue(
            InterpolationId,
            context
                .GetInterpolationInfo(InterpolationId)
                .Symbol
                !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );

        switch (IsCollection, IsBuilder)
        {
            case (false, false): return source;
            case (true, false): return $"..{source}";
            case (false, true):
                return $"new {
                    context.KnownTypes
                        .SelectMenuOptionBuilderType
                        !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                }({source})";
            case (true, true):
                return
                    $"""
                     ..{source}.Select(x => 
                         new {
                             context.KnownTypes
                                 .SelectMenuOptionBuilderType
                                 !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                         }(x)
                     )
                     """;
        }
    }

    public static bool TryCreate(
        IComponentContext context,
        ICXNode interpolation,
        out SelectMenuInterpolatedOption option
    )
    {
        CXToken interpolationToken;
        DesignerInterpolationInfo info;

        switch (interpolation)
        {
            case CXToken { Kind: CXTokenKind.Interpolation } token:
                info = context.GetInterpolationInfo(token);
                interpolationToken = token;
                break;
            case CXValue.Interpolation value:
                info = context.GetInterpolationInfo(value);
                interpolationToken = value.Token;
                break;
            default:
                option = null!;
                return false;
        }

        var symbol = info.Symbol;

        bool isCollection;

        // ReSharper disable once AssignmentInConditionalExpression
        if (isCollection = symbol.TryGetEnumerableType(out var innerSymbol))
            symbol = innerSymbol;

        var isBuilderType = context.Compilation.HasImplicitConversion(
            symbol,
            context.KnownTypes.SelectMenuOptionBuilderType
        );

        var isComponentType = context.Compilation.HasImplicitConversion(
            symbol,
            context.KnownTypes.SelectMenuOptionType
        );

        if (!isBuilderType && !isComponentType)
        {
            context.AddDiagnostic(
                Diagnostics.InvalidStringSelectChild,
                interpolation,
                info.Symbol!.ToDisplayString()
            );
            option = null!;
            return false;
        }

        option = new(
            interpolationToken,
            info.Id,
            isCollection,
            isBuilderType
        );
        return true;
    }
}