using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class ComponentTargetTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void InvalidModalComponent()
    {
        Graph(
            "<separator />",
            options: new(
                Target: ComponentTargetType.Modal
            )
        );
        {
            var separator = Component<SeparatorComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.ComponentTargetIsNotAllowed(separator, ComponentTargetType.Modal)
                );
            }
        }
    }
}