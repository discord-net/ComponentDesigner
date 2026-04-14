using ComponentDesigner;
using Discord;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

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
}