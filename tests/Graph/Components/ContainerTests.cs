using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class ContainerTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyContainer()
    {
        Graph(
            "<container />"
        );
        {
            var container = Component<ContainerComponentNode>(out var containerNode);
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.ComponentRequiresAtLeastOneChild(container), containerNode.State.TextSpan);
            }
        }
    }
}