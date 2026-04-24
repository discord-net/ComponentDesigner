using System.Collections.Immutable;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;

using Diagnostic = ComponentDesigner.Diagnostic;

namespace UnitTests.GeneratorTests;

public abstract class BaseGeneratorTest : TestWithDiagnostics
{
    public static readonly string[] AllTrackingNames = typeof(TrackingNames)
        .GetFields()
        .Where(fi => fi is { IsLiteral: true, IsInitOnly: false } && fi.FieldType == typeof(string))
        .Select(x => (string)x.GetRawConstantValue()!)
        .Where(x => !string.IsNullOrEmpty(x))
        .ToArray();

    private readonly ITestOutputHelper _output;
    private readonly SourceGenerator _generator;
    private GeneratorDriver _driver;
    private SyntaxTree? _tree;
    private Compilation _compilation;

    public BaseGeneratorTest(ITestOutputHelper output) : base(output)
    {
        _output = output;
        _generator = new SourceGenerator();

        _driver = CSharpGeneratorDriver.Create(
            [_generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );
        _compilation = Compilations.Create();
    }

    public GeneratorDriverRunResult RunCX(
        string cx,
        string? pretext = null,
        bool allowParsingErrors = false,
        GeneratorGraphOptions? options = null,
        string? additionalMethods = null,
        string testClassName = "TestClass",
        string testFuncName = "Run",
        bool hasInterpolations = true,
        int quoteCount = 3,
        string[]? trackingNames = null,
        bool pushDiagnostics = false
    )
    {
        var quotes = new string('"', quoteCount);
        var dollar = hasInterpolations ? "$" : string.Empty;
        var pad = hasInterpolations ? new(' ', dollar.Length) : string.Empty;
        var cxString = new StringBuilder();

        cxString.Append(dollar).Append(quotes);

        if (quoteCount >= 3)
        {
            cxString.AppendLine();
            cxString.Append(pad);
        }

        cxString.Append(quoteCount >= 3 ? cx.WithNewlinePadding(pad.Length) : cx);

        if (quoteCount >= 3)
        {
            cxString.AppendLine();
            cxString.Append(pad);
        }

        cxString.Append(quotes);

        var source =
            $$""""
              using Discord;
              using System.Collections.Generic;
              using System.Linq;

              public class {{testClassName}}
              {
                  public void {{testFuncName}}()
                  {
                      {{pretext}}
                      cx.any(
                          {{cxString.ToString().WithNewlinePadding(12)}},
                          autoRows: {{(options?.AllowAutoRows ?? false).ToString().ToLowerInvariant()}},
                          autoTextDisplays: {{(options?.AllowAutoTextDisplays ?? false).ToString().ToLowerInvariant()}}
                      );
                  }
                  {{additionalMethods?.WithNewlinePadding(4)}}
              }
              """";

        _output.WriteLine($"CX:\n{cxString}");

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        if (_tree is not null)
        {
            _compilation = _compilation.ReplaceSyntaxTree(_tree, syntaxTree);
            _tree = syntaxTree;
        }
        else
        {
            _compilation = _compilation.AddSyntaxTrees(_tree = syntaxTree);
        }

        var result = RunGeneratorsAndAssertOutput(_compilation, trackingNames ?? AllTrackingNames);

        if (pushDiagnostics)
        {
            base.PushDiagnostics(
                result.Diagnostics.Select(x => x.ToCX())
            );
        }


        return result;
    }

    private GeneratorDriverRunResult RunGeneratorsAndAssertOutput(
        Compilation compilation,
        string[]? trackingNames = null)
    {
        var clone = _compilation.Clone();
        _driver = _driver.RunGenerators(_compilation);

        var firstRunResult = _driver.GetRunResult();

        var secondRunResult = _driver
            .RunGenerators(clone)
            .GetRunResult();

        if (!secondRunResult.Results[0].TrackedOutputSteps.IsEmpty)
        {
            Assert.All(
                secondRunResult
                    .Results[0]
                    .TrackedOutputSteps
                    .SelectMany(x => x.Value)
                    .SelectMany(x => x.Outputs),
                x =>
                    Assert.Equal(IncrementalStepRunReason.Cached, x.Reason)
            );
        }

        return firstRunResult;
    }

    protected void AssertStepResult(GeneratorDriverRunResult result, string name, IncrementalStepRunReason step)
    {
        Assert.Equal(step, result.Results[0].TrackedSteps[name][0].Outputs[0].Reason);
    }

    protected void AssertRenders(GeneratorDriverRunResult result, string expected)
    {
        var rendered = result.Results[0]
            .TrackedSteps[TrackingNames.EMIT_GRAPH][0]
            .Outputs[0].Value as EmittedGraph;

        Assert.NotNull(rendered);
        Assert.NotEmpty(rendered.Renders);

        Assert.Equal(expected, string.Join(",\n", rendered.Renders.Select(x => x.Source)));
    }

    protected T GetStepValue<T>(GeneratorDriverRunResult result, string name)
        => (T)result.Results[0]
            .TrackedSteps
            .First(x => x.Key == name)
            .Value[0]
            .Outputs[0]
            .Value;

    protected void LogRunVisual(GeneratorDriverRunResult result)
    {
        var tree = _generator
            .Provider
            .ToDOTTree(
                result.Results[0]
                    .TrackedSteps
                    .ToDictionary(x => x.Key, x => x.Value[0].Outputs[0].Reason)
            );

        _output.WriteLine($"Generator Tree:\n{tree}");
    }

    private void AssertRunEquals(GeneratorDriverRunResult a, GeneratorDriverRunResult b, string[]? trackingNames)
    {
        if (trackingNames is null) return;

        var trackedSteps1 = GetTrackedSteps(a, trackingNames);
        var trackedSteps2 = GetTrackedSteps(b, trackingNames);

        if (trackingNames.Length is 0)
        {
            Assert.Empty(trackedSteps1);
            Assert.Empty(trackedSteps2);

            return;
        }

        Assert.Equal(trackedSteps1.Count, trackedSteps2.Count);
        Assert.True(trackedSteps1.Keys.ToHashSet().SetEquals(trackedSteps2.Keys));

        foreach (var (name, steps) in trackedSteps1)
        {
            var steps2 = trackedSteps2[name];

            Assert.Equal(steps.Length, steps2.Length);

            for (var i = 0; i < steps.Length; i++)
            {
                var stepA = steps[i];
                var stepB = steps2[i];

                Assert.Equal(
                    stepA.Outputs.Select(x => x.Value),
                    stepB.Outputs.Select(x => x.Value)
                );

                Assert.All(stepB.Outputs, x =>
                    Assert.True(x.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
                );
            }
        }


        static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
            GeneratorDriverRunResult runResult, string[]? trackingNames
        ) => runResult.Results[0]
            .TrackedSteps
            .Where(step => trackingNames.Contains(step.Key))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}