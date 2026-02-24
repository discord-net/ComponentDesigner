using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using ComponentDesigner.CSharp;
using Discord;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ComponentDesigner;

public sealed class SourceGenerator : IIncrementalGenerator
{
    private const string ENABLE_AUTO_ROWS_KEY = "build_property.EnableAutoRows";
    private const string ENABLE_AUTO_TEXT_DISPLAY = "build_property.EnableAutoTextDisplay";

    public static DiscordNetComponentDesignerImplementation Implementation { get; } = new();
    
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ComponentDesignerTarget> targetProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsComponentDesignerEntryPoint,
                MapPossibleComponentDesignerEntryPoint
            )
            .WithTrackingName(TrackingNames.INITIAL_TARGET)
            .Where(x => x is not null)!;

        var optionsProvider = CreateGraphOptionsProvider(context);

        var graphProvider = targetProvider
            .Combine(optionsProvider)
            .Select(CreateGraphParameters)
            .WithComparer(Stage1GraphParametersComparer.Instance)
            .Select(CreateGraph);
    }

    public static CXComponentGraph CreateGraph(GraphParameters parameters, CancellationToken cancellationToken)
        => CXComponentGraph.Create(parameters, cancellationToken);

    public static GraphParameters CreateGraphParameters(
        (ComponentDesignerTarget Target, GraphOptions Options) tuple,
        CancellationToken cancellationToken
    ) => new (
        Implementation,
        CSharpCompilationProvider.Get(tuple.Target.Compilation),
        tuple.Target.CX,
        tuple.Options
    );

    public static IncrementalValueProvider<GraphOptions> CreateGraphOptionsProvider(
        IncrementalGeneratorInitializationContext context
    )
    {
        return context
            .CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(CreateOptions);
        
        static GraphOptions CreateOptions(
            (Compilation Compilation, AnalyzerConfigOptionsProvider Options) tuple,
            CancellationToken cancellationToken
        )
        {
            var (compilation, options) = tuple;

            var autoRows = GetBoolValue(options, ENABLE_AUTO_ROWS_KEY);
            var autoTextDisplay = GetBoolValue(options, ENABLE_AUTO_TEXT_DISPLAY);

            return new(
                autoRows ?? GraphOptions.Default.AllowAutoRows,
                autoTextDisplay ?? GraphOptions.Default.AllowAutoTextDisplays
            );
        }

        static bool? GetBoolValue(AnalyzerConfigOptionsProvider provider, string key)
        {
            if (
                !provider.GlobalOptions.TryGetValue(key, out var val) ||
                string.IsNullOrEmpty(val) ||
                !bool.TryParse(val, out var flag)
            )  return null;

            return flag;
        }
    }

    public static ComponentDesignerTarget? MapPossibleComponentDesignerEntryPoint(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken
    ) => MapPossibleComponentDesignerEntryPoint(context.SemanticModel, context.Node, cancellationToken);

    public static ComponentDesignerTarget? MapPossibleComponentDesignerEntryPoint(
        SemanticModel semanticModel,
        SyntaxNode syntaxNode,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !TryGetValidDesignerCall(
                semanticModel,
                syntaxNode,
                cancellationToken,
                out var invocationOperation,
                out var invocationExpressionSyntax,
                out var interceptableLocation,
                out var cxArgumentExpressionSyntax
            ) ||
            !TryGetCXDesigner(
                cxArgumentExpressionSyntax,
                semanticModel,
                cancellationToken,
                out var cx,
                out var locationInfo,
                out var interpolations,
                out var quoteCount
            )
        ) return null;

        var parentKey = semanticModel
            .GetEnclosingSymbol(invocationExpressionSyntax.SpanStart, cancellationToken)
            ?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var designerParameter = invocationOperation
            .TargetMethod
            .Parameters[0];

        var usesDesignerParameter = designerParameter
            .Type
            .SpecialType is not SpecialType.System_String;

        var designerParameterName = usesDesignerParameter
            ? designerParameter.Name
            : null;

        return new ComponentDesignerTarget(
            semanticModel.Compilation,
            interceptableLocation,
            new CXModel(
                cx,
                locationInfo,
                quoteCount,
                usesDesignerParameter,
                designerParameterName,
                interpolations
            )
        );
    }

    public static bool TryGetCXDesigner(
        ExpressionSyntax cxExpressionSyntax,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [MaybeNullWhen(false)] out string content,
        [MaybeNullWhen(false)] out LocationInfo locationInfo,
        [MaybeNullWhen(false)] out InterpolationInfo[] interpolations,
        out int quoteCount
    )
    {
        var provider = CSharpCompilationProvider.Get(semanticModel.Compilation);

        switch (cxExpressionSyntax)
        {
            case LiteralExpressionSyntax { Token.Text: { } literalContent } literal:
                content = PrepareRawLiteral(literalContent, out var startQuoteCount, out var endQuoteCount);
                quoteCount = startQuoteCount;
                interpolations = [];
                locationInfo = LocationInfo.From(literal, cancellationToken);
                return true;

            case InterpolatedStringExpressionSyntax interpolated:
                content = interpolated.Contents.ToString();
                interpolations = interpolated
                    .Contents
                    .OfType<InterpolationSyntax>()
                    .Select((x, i) =>
                    {
                        var typeInfo = semanticModel.GetTypeInfo(x.Expression, cancellationToken);

                        return new InterpolationInfo(
                            i,
                            x.Span.AsCXTextSpan,
                            provider.GetTypeSymbol(typeInfo.Type),
                            semanticModel.GetConstantValue(x.Expression, cancellationToken).AsComponentDesignerOptional
                        );
                    })
                    .ToArray();
                locationInfo = LocationInfo.From(
                    cxExpressionSyntax.SyntaxTree.GetLocation(interpolated.Contents.Span),
                    cancellationToken
                );
                quoteCount = interpolated.StringEndToken.Span.Length;
                return true;
            default:
                content = null;
                locationInfo = null;
                interpolations = null;
                quoteCount = 0;
                return false;
        }

        static string PrepareRawLiteral(
            string literal,
            out int startQuoteCount,
            out int endQuoteCount
        )
        {
            for (startQuoteCount = 0; startQuoteCount < literal.Length; startQuoteCount++)
            {
                if (literal[startQuoteCount] is not '"') break;
            }

            endQuoteCount = 0;
            if (literal.Length == startQuoteCount)
            {
                return string.Empty;
            }

            for (var i = literal.Length - 1; i >= startQuoteCount; i--, endQuoteCount++)
                if (literal[i] is not '"')
                    break;

            return literal.Substring(
                startQuoteCount, literal.Length - startQuoteCount - endQuoteCount
            );
        }
    }

    public static bool TryGetValidDesignerCall(
        SemanticModel semanticModel,
        SyntaxNode syntaxNode,
        CancellationToken cancellationToken,
        [MaybeNullWhen(false)] out IInvocationOperation operation,
        [MaybeNullWhen(false)] out InvocationExpressionSyntax invocationSyntax,
        [MaybeNullWhen(false)] out InterceptableLocation interceptableLocation,
        [MaybeNullWhen(false)] out ExpressionSyntax cxArgumentExpressionSyntax
    )
    {
        var localOperation = semanticModel.GetOperation(syntaxNode, cancellationToken);

        interceptableLocation = null!;
        cxArgumentExpressionSyntax = null!;
        invocationSyntax = null!;

        checkOperation:
        switch (localOperation)
        {
            case IInvalidOperation invalid:
                localOperation = invalid.ChildOperations.OfType<IInvocationOperation>().FirstOrDefault()!;
                goto checkOperation;
            case IInvocationOperation invocation:
                if (
                    invocation
                        .TargetMethod
                        .ContainingType
                        .ToDisplayString()
                    is "Discord.ComponentDesigner"
                )
                {
                    operation = invocation;
                    break;
                }

                goto default;

            default:
            {
                operation = null!;
                return false;
            }
        }

        if (syntaxNode is not InvocationExpressionSyntax syntax) return false;

        invocationSyntax = syntax;

        if (semanticModel.GetInterceptableLocation(invocationSyntax, cancellationToken) is not { } location)
            return false;

        interceptableLocation = location;

        if (invocationSyntax.ArgumentList.Arguments.Count < 1) return false;

        cxArgumentExpressionSyntax = invocationSyntax.ArgumentList.Arguments[0].Expression;

        return true;
    }

    public static bool IsComponentDesignerEntryPoint(SyntaxNode node, CancellationToken token)
        => node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: { Identifier.Value: "Create" or "cx" }
            } or IdentifierNameSyntax
            {
                Identifier.ValueText: "cx"
            }
        };
}