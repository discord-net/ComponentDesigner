using ComponentDesigner;
using Discord;
using UnitTests.Graph.Components;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public abstract class BaseDiscordNetComponentTest(ITestOutputHelper output) : BaseComponentTest(output)
{
    protected override GraphParameters CreateGraphParameters(
        ICompilationProvider compilationProvider,
        ICXModel cxModel,
        IGraphOptions? options
    ) => new(
        new DiscordNetComponentDesignerImplementation(),
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
            DiscordNetRenderer.Instance,
            cancellationToken
        )
        .Map(renders =>
            string.Join($",{Environment.NewLine}", renders.Select(x => x.Source))
        );
}