using System.Text.Json;
using ComponentDesigner;
using ComponentDesigner.Json;
using Microsoft.CodeAnalysis;
using UnitTests.Graph.Components;
using Xunit.Abstractions;

namespace UnitTests.Json;

public abstract class BaseJsonComponentTest(ITestOutputHelper output) : BaseComponentTest(output)
{
    protected override GraphParameters CreateGraphParameters(
        ICompilationProvider compilationProvider,
        ICXModel cxModel,
        IGraphOptions? options
    ) => new(
        JsonComponentImplementation.Instance,
        compilationProvider,
        cxModel,
        options ?? GeneratorGraphOptions.Default
    );

    protected override Result<string> EmitGraph(
        CXComponentGraph graph,
        ICompilationProvider compilationProvider,
        CancellationToken cancellationToken = default
    ) => graph
        .Emit(
            compilationProvider,
            JsonRenderer.Instance,
            cancellationToken
        )
        .Map(node => node
            .ToJsonString(
                new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    IndentSize = 4
                }
            )
        );
}