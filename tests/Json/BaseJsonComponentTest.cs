using System.Text.Json;
using ComponentDesigner;
using ComponentDesigner.Json;
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
        new JsonComponentImplementation(
            new JsonSerializerOptions()
            {
                WriteIndented = true,
                IndentSize = 4
            }
        ),
        compilationProvider,
        cxModel,
        options ?? GeneratorGraphOptions.Default
    );
}