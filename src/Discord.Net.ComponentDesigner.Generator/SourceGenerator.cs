using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Discord.CX.Util;

namespace Discord.CX;

using UpdateGraphStateParams =
    (
    ImmutableArray<CXGraph> Left,
    (ImmutableArray<ComponentDesignerTarget?> Left, EquatableArray<string> Right) Right
    );

[Generator]
public sealed partial class SourceGenerator : IIncrementalGenerator
{
    public IncrementalValueProvider<(ImmutableArray<RenderedGraph> Left, ImmutableArray<Glue> Right)> Provider
    {
        get;
        private set;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsMethodCall,
                MapPossibleComponentDesignerCall
            )
            .WithTrackingName(TrackingNames.INITIAL_TARGET)
            .Where(x => x is not null)
            .WithTrackingName(TrackingNames.FILTER_NOT_NULL_TARGETS)
            .Collect()
            .WithTrackingName(TrackingNames.ALL_TARGETS);

        var keyMapperProvider = targetProvider
            .Select(GetKeys)
            .WithTrackingName(TrackingNames.MAP_KEYS);

        var combinedKeys = targetProvider
            .Combine(keyMapperProvider)
            .WithTrackingName(TrackingNames.KEYS_AND_TARGETS);

        var glueProvider =
            combinedKeys
                .SelectMany(CreateClue)
                .WithTrackingName(TrackingNames.CREATE_GLUE);

        var graphProvider = combinedKeys
            .Combine(GeneratorOptions.CreateProvider(context))
            .WithTrackingName(TrackingNames.GRAPH_STATE_PARAMETERS)
            .SelectMany(CreateGraphState)
            .WithTrackingName(TrackingNames.CREATE_GRAPH_STATE)
            .Select(CreateGraph)
            .WithTrackingName(TrackingNames.CREATE_GRAPH);

        // inject compilation
        graphProvider = graphProvider
            .Collect()
            .WithTrackingName(TrackingNames.ALL_GRAPHS)
            .Combine(combinedKeys)
            .WithTrackingName(TrackingNames.ALL_GRAPHS_AND_KEYS)
            .SelectMany(UpdateGraphState)
            .WithTrackingName(TrackingNames.UPDATE_GRAPH_STATE);

        Provider = graphProvider
            .Select(RenderGraph)
            .WithTrackingName(TrackingNames.RENDER_GRAPH)
            .Collect()
            .WithTrackingName(TrackingNames.ALL_RENDERS)
            .Combine(glueProvider.Collect().WithTrackingName(TrackingNames.ALL_GLUE))
            .WithTrackingName(TrackingNames.RENDER_AND_GLUE_EMIT);

        context.RegisterSourceOutput(
            Provider,
            Generate
        );
    }

    public static IEnumerable<Glue> CreateClue(
        (ImmutableArray<ComponentDesignerTarget?> Targets, EquatableArray<string> Keys) tuple,
        CancellationToken token
    )
    {
        var (targets, keys) = tuple;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var key = keys[i];

            if (target is null || string.IsNullOrWhiteSpace(key)) continue;

            yield return new Glue(
                target.Compilation,
                key,
                target.InterceptLocation,
                target.CX.Location,
                target.CX.UsesDesignerParameter,
                target.SyntaxTree,
                target.Overloads,
                target.CX.Kind
            );
        }
    }

    public static RenderedGraph RenderGraph(CXGraph graph, CancellationToken token)
        => graph.Render(token: token);

    public static IEnumerable<CXGraph> UpdateGraphState(
        UpdateGraphStateParams tuple,
        CancellationToken token
    )
    {
        var (graphs, (targets, keys)) = tuple;

        // at this point in the pipeline, nothing should be removed from the provider arrays, any non-mapped target is
        // null across graphs, targets, and keys
        Debug.Assert(
            graphs.Length == targets.Length &&
            targets.Length == keys.Count
        );

        for (var i = 0; i < graphs.Length; i++)
        {
            var graph = graphs[i];
            var target = targets[i];
            var key = keys[i];

            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (graph is null || target is null || key is null)
            {
                Debug.Fail("Pipeline contains null values");
                continue;
            }
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            yield return graph.Update(target!, token);
        }
    }

    public static CXGraph CreateGraph(GraphGeneratorState state, CancellationToken token)
    {
        // TODO: incremental parsing
        return CXGraph.Create(state, old: null, token);
    }

    public static IEnumerable<GraphGeneratorState> CreateGraphState(
        ((ImmutableArray<ComponentDesignerTarget?> Left, EquatableArray<string> Right) Left, GeneratorOptions Right)
            tuple,
        CancellationToken token)
    {
        var ((targets, keys), options) = tuple;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var key = keys[i];

            if (target is null || string.IsNullOrWhiteSpace(key)) continue;

            yield return CreateGraphState(key, target, options);
        }
    }

    public static GraphGeneratorState CreateGraphState(
        string key,
        ComponentDesignerTarget target,
        GeneratorOptions options
    ) => new(
        key,
        options.WithOverloads(target.Overloads),
        target.CX
    );


    public void Generate(
        SourceProductionContext context,
        (ImmutableArray<RenderedGraph> Renders, ImmutableArray<Glue> Glues) tuple
    )
    {
        var (renders, glues) = tuple;

        if (renders.IsEmpty) return;

        Debug.Assert(renders.Length == glues.Length);

        var sb = new StringBuilder();

        for (var i = 0; i < renders.Length; i++)
        {
            var render = renders[i];
            var glue = glues[i];

            Debug.Assert(render.Key == glue.Key);

            var knownTypes = glue.Compilation.GetKnownTypes();

            var returnType = (
                glue.Kind switch
                {
                    CXKind.Modal => knownTypes.CXModalComponentType,
                    CXKind.Message => knownTypes.CXMessageComponentType,
                    _ => knownTypes.CXComponentType,
                }
            )?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            
            foreach (var diagnosticInfo in glue.Overloads.Diagnostics)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        diagnosticInfo.Descriptor,
                        glue.SyntaxTree.GetLocation(diagnosticInfo.Span)
                    )
                );
            }

            foreach (var diagnostic in render.Diagnostics)
            {
                // adjust the span to match the source
                var start = glue.Location.TextSpan.Start + diagnostic.Span.Start;

                if (diagnostic.Span.IsEmpty)
                    start--;

                var diagnosticSpan = new TextSpan(
                    start,
                    Math.Max(1, diagnostic.Span.Length)
                );

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        diagnostic.Descriptor,
                        glue.SyntaxTree.GetLocation(diagnosticSpan)
                    )
                );
            }

            var body = string.IsNullOrWhiteSpace(render.EmittedSource)
                ? $"{returnType}.Empty"
                : $"""
                   new {returnType}([
                       {render.EmittedSource!.WithNewlinePadding(4)}
                   ])
                   """;

            if (i > 0)
                sb.AppendLine();

            var parameter = glue.UsesDesigner
                ? $"global::{Constants.COMPONENT_DESIGNER_QUALIFIED_NAME} designer"
                : "global::System.String cx";

            sb.AppendLine(
                $$"""
                  /*
                  {{glue.Location}}

                  {{render.CX.NormalizeIndentation()}}
                  */
                  [global::System.Runtime.CompilerServices.InterceptsLocation(version: {{glue.InterceptLocation.Version}}, data: "{{glue.InterceptLocation.Data}}")]
                  public static {{returnType}} _{{Math.Abs(glue.InterceptLocation.GetHashCode())}}(
                      {{parameter}},
                      bool? autoRows = null,
                      bool? autoTextDisplays = null
                  ) => {{body}};
                  """
            );
        }

        context.AddSource(
            "Interceptors.g.cs",
            $$"""
              using Discord;

              namespace System.Runtime.CompilerServices
              {
                  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                  sealed file class InterceptsLocationAttribute(int version, string data) : Attribute;
              }

              namespace InlineComponent
              {
                  static file class Interceptors
                  {
                      {{sb.ToString().WithNewlinePadding(8)}}
                  }
              }
              """
        );
    }

    public static EquatableArray<string> GetKeys(
        ImmutableArray<ComponentDesignerTarget?> target,
        CancellationToken token
    )
    {
        var result = new string[target.Length];

        var map = new Dictionary<string, int>();
        var globalCount = 0;

        for (var i = 0; i < target.Length; i++)
        {
            var targetItem = target[i];

            if (targetItem is null) continue;

            string key;
            if (targetItem.ParentKey is null)
            {
                key = $"<global>:{globalCount++}";
            }
            else
            {
                map.TryGetValue(targetItem.ParentKey, out var index);

                key = $"{targetItem.ParentKey}:{index}";
                map[targetItem.ParentKey] = index + 1;
            }

            result[i] = key;
        }

        return [..result];
    }

    public static ComponentDesignerTarget? MapPossibleComponentDesignerCall(
        GeneratorSyntaxContext context,
        CancellationToken token
    ) => MapPossibleComponentDesignerCall(context.SemanticModel, context.Node, token);

    public static ComponentDesignerTarget? MapPossibleComponentDesignerCall(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken token
    )
    {
        if (
            !TryGetValidDesignerCall(
                out var operation,
                out var invocationSyntax,
                out var interceptLocation,
                out var argumentSyntax,
                out var kind
            )
        ) return null;

        if (
            !TryGetCXDesigner(
                argumentSyntax,
                semanticModel,
                out var cxDesigner,
                out var locationInfo,
                out var interpolationInfos,
                out var quoteCount,
                token
            )
        ) return null;


        return new ComponentDesignerTarget(
            interceptLocation,
            semanticModel
                .GetEnclosingSymbol(invocationSyntax.SpanStart, token)
                ?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            new CXDesignerGeneratorState(
                cxDesigner,
                locationInfo,
                quoteCount,
                operation.TargetMethod.Parameters[0].Type.SpecialType is not SpecialType.System_String,
                [..interpolationInfos],
                semanticModel,
                invocationSyntax.SyntaxTree,
                kind
            ),
            GetOptionOverloads(operation, semanticModel, token)
        );

        static ComponentDesignerOptionOverloads GetOptionOverloads(
            IInvocationOperation operation,
            SemanticModel semanticModel,
            CancellationToken token
        )
        {
            var enableAutoRows = Result<bool>.Empty;
            var enableAutoTextDisplays = Result<bool>.Empty;

            foreach (var argument in operation.Arguments)
            {
                if (argument.ArgumentKind is ArgumentKind.DefaultValue) continue;

                switch (argument.Parameter?.Name)
                {
                    case "autoRows" when argument.Syntax is ArgumentSyntax { Expression: { } expression }:
                        enableAutoRows = GetConstantValue(expression);
                        break;
                    case "autoTextDisplays" when argument.Syntax is ArgumentSyntax { Expression: { } expression }:
                        enableAutoTextDisplays = GetConstantValue(expression);
                        break;
                }
            }

            return new(enableAutoRows, enableAutoTextDisplays);

            Result<bool> GetConstantValue(ExpressionSyntax expression)
            {
                var value = semanticModel.GetConstantValue(expression, token);

                if (!value.HasValue)
                {
                    return new DiagnosticInfo(
                        Diagnostics.ExpectedAConstantValue,
                        expression.Span
                    );
                }

                return value.Value switch
                {
                    true or false => new((bool)value.Value),
                    _ => Result<bool>.Empty
                };
            }
        }

        static bool TryGetCXDesigner(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out string content,
            out LocationInfo locationInfo,
            out DesignerInterpolationInfo[] interpolations,
            out int quoteCount,
            CancellationToken token
        )
        {
            switch (expression)
            {
                case LiteralExpressionSyntax { Token.Text: { } literalContent } literal:
                    content = PrepareRawLiteral(
                        literalContent,
                        out var startQuoteCount,
                        out var endQuoteCount
                    );

                    quoteCount = startQuoteCount;
                    interpolations = [];
                    locationInfo = LocationInfo.CreateFrom(
                        expression.SyntaxTree.GetLocation(
                            TextSpan.FromBounds(
                                literal.Token.Span.Start + startQuoteCount,
                                literal.Token.Span.End - endQuoteCount
                            )
                        )
                    )!;
                    return true;

                case InterpolatedStringExpressionSyntax interpolated:
                    content = interpolated.Contents.ToString();
                    interpolations = interpolated.Contents
                        .OfType<InterpolationSyntax>()
                        .Select((x, i) =>
                        {
                            var typeInfo = semanticModel.GetTypeInfo(x.Expression, token);

                            return new DesignerInterpolationInfo(
                                i,
                                x.FullSpan,
                                typeInfo.Type ?? typeInfo.ConvertedType,
                                semanticModel.GetConstantValue(x.Expression, token)
                            );
                        })
                        .ToArray();
                    locationInfo = LocationInfo.CreateFrom(
                        expression.SyntaxTree.GetLocation(interpolated.Contents.Span)
                    )!;
                    quoteCount = interpolated.StringEndToken.Span.Length;
                    return true;
                default:
                    content = string.Empty;
                    locationInfo = null!;
                    interpolations = [];
                    quoteCount = 0;
                    return false;
            }
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

        bool TryGetValidDesignerCall(
            out IInvocationOperation operation,
            out InvocationExpressionSyntax invocationSyntax,
            out InterceptableLocation interceptLocation,
            out ExpressionSyntax argumentExpressionSyntax,
            out CXKind kind
        )
        {
            var localOperation = semanticModel.GetOperation(node, token)!;

            kind = CXKind.Unknown;
            interceptLocation = null!;
            argumentExpressionSyntax = null!;
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

            if (node is not InvocationExpressionSyntax syntax) return false;

            invocationSyntax = syntax;

            if (semanticModel.GetInterceptableLocation(invocationSyntax, token) is not { } location)
                return false;

            interceptLocation = location;

            if (invocationSyntax.ArgumentList.Arguments.Count < 1) return false;

            argumentExpressionSyntax = invocationSyntax.ArgumentList.Arguments[0].Expression;

            return IsValidComponentDesignerEntryPoint(semanticModel.Compilation, operation.TargetMethod, out kind);
        }
    }

    public static bool IsValidComponentDesignerEntryPoint(
        Compilation compilation,
        IMethodSymbol symbol,
        out CXKind kind
    )
    {
        kind = CXKind.Unknown;

        var knownTypes = compilation.GetKnownTypes();

        if (knownTypes.CXEntryPointAttribute is not { } targetAttributeSymbol) return false;

        if (
            symbol
            .GetAttributes()
            .All(x =>
                !targetAttributeSymbol.Equals(x.AttributeClass, SymbolEqualityComparer.Default)
            )
        ) return false;

        if (symbol.ReturnType.Equals(knownTypes.CXComponentType, SymbolEqualityComparer.Default))
            kind = CXKind.Any;
        else if (symbol.ReturnType.Equals(knownTypes.CXMessageComponentType, SymbolEqualityComparer.Default))
            kind = CXKind.Message;
        else if (symbol.ReturnType.Equals(knownTypes.CXModalComponentType, SymbolEqualityComparer.Default))
            kind = CXKind.Modal;

        return kind is not CXKind.Unknown;
    }

    public static bool IsMethodCall(SyntaxNode node, CancellationToken token)
        => node is InvocationExpressionSyntax;
}