using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class FunctionalTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void InstancedComponent()
    {
        Graph(
            "<{inst}.MyFunc />",
            hasInterpolations: true,
            pretext: "var inst = new TestInstanceComponent();",
            additionalMembers:
            """
            public class TestInstanceComponent
            {
                public CXMessageComponent MyFunc() => CXMessageComponent.Empty;    
            }
            """
        );
        {
            Component<FunctionalComponentNode>();

            Emits("designer.GetValue<global::TestClass.TestInstanceComponent>(0).MyFunc()");
        }
    }
}