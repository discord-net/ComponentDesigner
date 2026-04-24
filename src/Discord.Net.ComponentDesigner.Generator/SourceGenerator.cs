using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;
using Discord;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using RoslynDiagnostic = Microsoft.CodeAnalysis.Diagnostic;
using RoslynDiagnosticDescriptor = Microsoft.CodeAnalysis.DiagnosticDescriptor;

namespace ComponentDesigner;

using FinalProduct = (ImmutableArray<EmittedGraph>, ImmutableArray<ComponentDesignerTarget>);

[Generator]
public sealed class SourceGenerator : IIncrementalGenerator
{
    private const string ENABLE_AUTO_ROWS_KEY = "build_property.EnableAutoRows";
    private const string ENABLE_AUTO_TEXT_DISPLAY = "build_property.EnableAutoTextDisplay";

    public IncrementalValueProvider<FinalProduct> Provider { get; private set; }

    public static DiscordNetComponentDesignerImplementation Implementation { get; } = new();

    private readonly IncrementalGraphManager _manager = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ComponentDesignerTarget> targetProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsComponentDesignerEntryPoint,
                MapPossibleComponentDesignerEntryPoint
            )
            .WithTrackingName(TrackingNames.INITIAL_TARGET)
            .Where(x => x is not null)
            .WithTrackingName(TrackingNames.FILTER_NOT_NULL_TARGETS)!;

        var parametersProvider = targetProvider
            .Combine(CreateGraphOptionsProvider(context))
            .WithTrackingName(TrackingNames.TARGET_WITH_GENERATOR_OPTIONS)
            .Select(CreateGraphParameters)
            .WithTrackingName(TrackingNames.CREATE_GRAPH_PARAMETERS)
            .Collect()
            .SelectMany(_manager.Update);

        Provider = parametersProvider
            .Combine(context.CompilationProvider)
            .Select(_manager.UpdateGraphDependencies)
            .Select(EmitGraph)
            .WithTrackingName(TrackingNames.EMIT_GRAPH)
            .Collect()
            .WithTrackingName(TrackingNames.ALL_EMITTED_GRAPHS)
            .Combine(
                targetProvider
                    .Collect()
                    .WithTrackingName(TrackingNames.ALL_TARGETS)
            )
            .WithTrackingName(TrackingNames.EMITTED_GRAPHS_AND_TARGETS);

        // var graphProvider = parametersProvider
        //     .Select(CreateGraph)
        //     .WithTrackingName(TrackingNames.CREATE_GRAPH);


        // Provider = targetProvider
        //     .Combine(CreateGraphOptionsProvider(context))
        //     .WithTrackingName(TrackingNames.TARGET_WITH_GENERATOR_OPTIONS)
        //     .Select(CreateGraphParameters)
        //     .WithTrackingName(TrackingNames.CREATE_GRAPH_PARAMETERS)
        //     .WithComparer(Stage1GraphParametersComparer.Instance)
        //     .Select(CreateGraph)
        //     .WithTrackingName(TrackingNames.CREATE_GRAPH)
        //     .Combine(context.CompilationProvider)
        //     .WithTrackingName(TrackingNames.GRAPH_WITH_COMPILATION)
        //     .Select(UpdateGraphDependencies)
        //     .WithTrackingName(TrackingNames.UPDATE_GRAPH_EXTERNAL_DEPENDENCIES)
        //     .Select(EmitGraph)
        //     .WithTrackingName(TrackingNames.EMIT_GRAPH)
        //     .Collect()
        //     .WithTrackingName(TrackingNames.ALL_EMITTED_GRAPHS)
        //     .Combine(
        //         targetProvider
        //             .Collect()
        //             .WithTrackingName(TrackingNames.ALL_TARGETS)
        //     )
        //     .WithTrackingName(TrackingNames.EMITTED_GRAPHS_AND_TARGETS);

        context.RegisterImplementationSourceOutput(
            Provider,
            Generate
        );
    }


    private void Generate(
        SourceProductionContext context,
        FinalProduct product
    )
    {
        var (emits, targets) = product;

        if (emits.Length is 0) return;

        // since we don't remove/add any new values in the pipeline, the arrays should map 1<->1 
        if (emits.Length != targets.Length)
        {
            throw new InvalidOperationException("bad pipeline state");
        }

        var buckets = new Dictionary<string, List<string>>();

        for (var i = 0; i < emits.Length; i++)
        {
            var emitted = emits[i];
            var target = targets[i];

            foreach (var diagnostic in emitted.Diagnostics)
            {
                var location = GetNormalizedLocation(
                    emitted.Graph.Document.Source!,
                    target.CX.Location,
                    diagnostic.TextSpan
                );

                context.ReportDiagnostic(
                    diagnostic.ToRoslyn(location)
                );
            }

            if (emitted.Renders.Count is 0) continue;

            var bucketName = target.CX.Location.FilePath ?? string.Empty;

            if (!buckets.TryGetValue(bucketName, out var bucket))
                buckets[bucketName] = bucket = new();

            bucket.Add(
                RenderInterceptor(
                    emitted.Source,
                    target.InterceptableMethodInfo.Location,
                    target.InterceptableMethodInfo.ReturnType,
                    target.InterceptableMethodInfo.Parameters
                )
            );
        }

        var rootDir = ((GeneratorGraphOptions)emits[0].Graph.Options).ProjectDirectory;

        if (rootDir is null)
        {
            rootDir = CalculateCommonPath(buckets.Keys);
        }

        var sources = new Dictionary<string, List<string>>();

        foreach (var bucket in buckets)
        {
            string fileName;

            if (rootDir is null || string.IsNullOrEmpty(bucket.Key))
            {
                fileName = "Generated.g.cs";
            }
            else
            {
                var path = bucket.Key.StartsWith(rootDir)
                    ? bucket.Key.Substring(rootDir.Length)
                    : bucket.Key;

                // replace file ext with .g.cs

                var newFileName = $"{Path.GetFileNameWithoutExtension(path)}.g.cs";

                fileName = Path.GetDirectoryName(path) is { } dir
                    ? Path.Combine(
                        dir,
                        newFileName
                    )
                    : newFileName;
            }

            if (!sources.TryGetValue(fileName, out var fileBucket))
                sources[fileName] = fileBucket = [];

            fileBucket.AddRange(bucket.Value);
        }

        foreach (var source in sources)
        {
            context.AddSource(
                source.Key,
                $$"""
                  namespace System.Runtime.CompilerServices
                  {
                      [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                      sealed file class InterceptsLocationAttribute(int version, string data) : Attribute;
                  }

                  namespace ComponentDesignerInterceptors
                  {
                      static file class Interceptors
                      {
                          {{
                              string.Join(
                                  $"{Environment.NewLine}{Environment.NewLine}        ",
                                  source.Value.Select(x => x.WithNewlinePadding(8))
                              )
                          }}
                      }
                  }
                  """
            );
        }


        static string RenderInterceptor(
            string source,
            InterceptableLocation location,
            string returnType,
            string parameters
        ) => $"""
              [global::System.Runtime.CompilerServices.InterceptsLocation(version: {location.Version}, data: "{location.Data}")]
              public static {returnType} _{Math.Abs(location.GetHashCode())}({parameters})
                  => new {returnType}([
                      {source.WithNewlinePadding(8)}
                  ]);
              """;


        static string? CalculateCommonPath(IReadOnlyCollection<string> paths)
        {
            var minSlash = int.MaxValue;
            string? minPath = null;

            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;

                var splits = path.Split('\\').Count();
                if (minSlash > splits)
                {
                    minSlash = splits;
                    minPath = path;
                }
            }

            if (minPath != null)
            {
                var splits = minPath.Split(Path.DirectorySeparatorChar);
                for (var i = 0; i < minSlash; i++)
                {
                    if (paths.Any(x => !string.IsNullOrEmpty(x) && !x.StartsWith(splits[i])))
                    {
                        return i >= 0 ? splits.Take(i).ToString() : "";
                    }
                }
            }

            return minPath;
        }

        static Location GetNormalizedLocation(
            CXSourceText source,
            LocationInfo cxInfo,
            CXTextSpan textSpan
        )
        {
            var sourceLineSpan = source.Lines.GetLinePositionSpan(textSpan);

            // we have to normalize everything back to the original C# file
            return Location.Create(
                cxInfo.FilePath ?? string.Empty,
                new TextSpan(
                    textSpan.Start + cxInfo.TextSpan.Start,
                    textSpan.Length
                ),
                new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                    new Microsoft.CodeAnalysis.Text.LinePosition(
                        cxInfo.LineSpan.Start.Line + sourceLineSpan.Start.Line,
                        sourceLineSpan.Start.Character
                    ),
                    new Microsoft.CodeAnalysis.Text.LinePosition(
                        cxInfo.LineSpan.Start.Line + sourceLineSpan.End.Line,
                        sourceLineSpan.End.Character
                    )
                )
            );
        }
    }

    public static EmittedGraph EmitGraph(
        UpdatedGraph parameters,
        CancellationToken cancellationToken
    )
    {
        var result = parameters
            .Graph
            .Emit(
                parameters.CompilationProvider,
                DiscordNetRenderer.Instance,
                cancellationToken
            );

        return new(
            parameters.Graph,
            result.GetValueOrDefault([]),
            [..parameters.Graph.Diagnostics, ..result.Diagnostics],
            parameters.CompilationProvider
        );
    }

    public static CXComponentGraph CreateGraph(GraphParameters parameters, CancellationToken cancellationToken)
        => CXComponentGraph.Create(parameters, cancellationToken);

    public static GraphParameters CreateGraphParameters(
        (ComponentDesignerTarget Target, GeneratorGraphOptions Options) tuple,
        CancellationToken cancellationToken
    ) => new(
        Implementation,
        CSharpCompilationProvider.Get(tuple.Target.Compilation),
        tuple.Target.CX,
        tuple.Options.WithOverloads(tuple.Target.Overloads, tuple.Target.ComponentTargetType)
    );

    public static IncrementalValueProvider<GeneratorGraphOptions> CreateGraphOptionsProvider(
        IncrementalGeneratorInitializationContext context
    )
    {
        return context
            .CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(CreateOptions)
            .WithTrackingName(TrackingNames.GENERATOR_OPTIONS);

        static GeneratorGraphOptions CreateOptions(
            (Compilation Compilation, AnalyzerConfigOptionsProvider Options) tuple,
            CancellationToken cancellationToken
        )
        {
            var (compilation, options) = tuple;

            var autoRows = GetBoolValue(options, ENABLE_AUTO_ROWS_KEY);
            var autoTextDisplay = GetBoolValue(options, ENABLE_AUTO_TEXT_DISPLAY);

            string? projectDir = null;

            if (
                (
                    options.GlobalOptions.TryGetValue("build_property.ProjectDir", out var dir) &&
                    !string.IsNullOrEmpty(dir)
                ) ||
                (options.GlobalOptions.TryGetValue("build_property.SolutionDir", out dir) && !string.IsNullOrEmpty(dir))
            ) projectDir = dir;

            return new(
                autoRows ?? false,
                autoTextDisplay ?? false,
                projectDir
            );
        }

        static bool? GetBoolValue(AnalyzerConfigOptionsProvider provider, string key)
        {
            if (
                !provider.GlobalOptions.TryGetValue(key, out var val) ||
                string.IsNullOrEmpty(val) ||
                !bool.TryParse(val, out var flag)
            ) return null;

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

        var parentKey = GetParentKey();

        var designerParameter = invocationOperation
            .TargetMethod
            .Parameters[0];

        var usesDesignerParameter = designerParameter
            .Type
            .SpecialType is not SpecialType.System_String;

        var designerParameterName = usesDesignerParameter
            ? designerParameter.Name
            : null;

        var designerParameterType = usesDesignerParameter
            ? designerParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;

        if (!TryGetTarget(invocationOperation.TargetMethod.Name, out var target))
            return null;

        return new ComponentDesignerTarget(
            semanticModel.Compilation,
            new InterceptableMethodInfo(
                interceptableLocation,
                invocationOperation
                    .TargetMethod
                    .ReturnType
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ToParametersString(invocationOperation.TargetMethod)
            ),
            new CXModel(
                parentKey,
                cx,
                locationInfo,
                quoteCount,
                usesDesignerParameter,
                designerParameterName,
                designerParameterType,
                interpolations
            ),
            GetOptionOverloads(invocationOperation, semanticModel, cancellationToken),
            target
        );

        static bool TryGetTarget(
            string name,
            out ComponentTargetType target
        ) => (target = name switch
        {
            "message" => ComponentTargetType.Message,
            "modal" => ComponentTargetType.Modal,
            "any" => ComponentTargetType.Any,
            _ => ComponentTargetType.None
        }) is not ComponentTargetType.None;

        static GraphOptionsOverloads GetOptionOverloads(
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
                    return new Diagnostic(
                        expression.Span.AsCXTextSpan,
                        Diagnostic.ExpectedAConstantValue
                    );
                }

                return value.Value switch
                {
                    true or false => new((bool)value.Value),
                    _ => Result<bool>.Empty
                };
            }
        }

        string GetParentKey()
        {
            using var _ = StringBuilder.Pooled(out var sb);

            var symbol = semanticModel
                .GetEnclosingSymbol(invocationExpressionSyntax.SpanStart, cancellationToken);

            while (symbol is not null)
            {
                if (sb.Length > 0) sb.Insert(0, "::");

                sb.Insert(0, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

                symbol = symbol.ContainingSymbol;
            }

            return sb.ToString();
        }

        static string ToParametersString(IMethodSymbol symbol)
        {
            var sb = new StringBuilder();

            if (!symbol.IsStatic)
            {
                sb.Append(
                    $"this {symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} __this__"
                );
            }

            for (var i = 0; i < symbol.Parameters.Length; i++)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                var parameter = symbol.Parameters[i];

                if (parameter.ScopedKind is not ScopedKind.None)
                    sb.Append("scoped ");

                sb.Append(parameter.RefKind switch
                {
                    RefKind.In => "in ",
                    RefKind.Out => "out ",
                    RefKind.Ref => "ref ",
                    RefKind.RefReadOnlyParameter => "ref readonly",
                    _ => string.Empty
                });

                sb.Append(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .Append(' ')
                    .Append(parameter.Name);
            }

            return sb.ToString();
        }
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
                locationInfo = LocationInfo.From(
                    cxExpressionSyntax.SyntaxTree.GetLocation(interpolated.Contents.Span),
                    cancellationToken
                );
                interpolations = interpolated
                    .Contents
                    .OfType<InterpolationSyntax>()
                    .Select((x, i) =>
                    {
                        var typeInfo = semanticModel.GetTypeInfo(x.Expression, cancellationToken);

                        return new InterpolationInfo(
                            i,
                            new(
                                x.Span.Start - interpolated.Contents.Span.Start,
                                x.Span.Length
                            ),
                            provider.GetTypeSymbol(typeInfo.Type),
                            semanticModel
                                .GetConstantValue(x.Expression, cancellationToken)
                                .AsComponentDesignerOptional
                        );
                    })
                    .ToArray();
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
                operation = invocation;
                break;

            default:
            {
                operation = null!;
                return false;
            }
        }

        if (!IsValidCXMethod(operation.TargetMethod)) return false;

        if (syntaxNode is not InvocationExpressionSyntax syntax) return false;

        invocationSyntax = syntax;

        if (semanticModel.GetInterceptableLocation(invocationSyntax, cancellationToken) is not { } location)
            return false;

        interceptableLocation = location;

        if (invocationSyntax.ArgumentList.Arguments.Count < 1) return false;

        cxArgumentExpressionSyntax = invocationSyntax.ArgumentList.Arguments[0].Expression;

        return true;
    }

    public static bool IsValidCXMethod(IMethodSymbol method)
    {
        if (
            method.Name is not "any" and not "message" and not "modal" ||
            method.ContainingType is null or not { Name: "cx" } ||
            method.Parameters.Length is not 3
        ) return false;

        return
            method.Parameters[0] is { Type.Name: "CXSyntax" } &&
            method.Parameters[1] is { Name: "autoRows" } &&
            method.Parameters[2] is { Name: "autoTextDisplays" };
    }

    public static bool IsComponentDesignerEntryPoint(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocationSyntax) return false;

        return invocationSyntax.Expression
            is MemberAccessExpressionSyntax { Name.Identifier.Value: "any" or "message" or "modal" }
            or IdentifierNameSyntax { Identifier.ValueText: "any" or "message" or "modal" };
    }
}