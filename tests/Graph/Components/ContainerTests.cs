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

    [Fact]
    public void ContainerWithProperties()
    {
        Graph(
            """
            <container
                id='123'
                accentColor='red'
                spoiler
                unknown
            >
                <separator />
            </container>
            """
        );
        {
            var container = Component<ContainerComponentNode>(out var containerNode);
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    id: 123,
                    accentColor: global::Discord.Color.Red,
                    isSpoiler: true,
                    components: 
                    [
                        new global::Discord.SeparatorBuilder()
                    ]
                )
                """
            );
            {
                AssertDiagnostic(Diagnostic.UnknownPropertyOfComponent(container, "unknown"));
            }
        }
    }

    [Fact]
    public void ContainerWithInterpolatedProperties()
    {
        Graph(
            """
            <container
                id={id}
                accentColor={color}
                spoiler={spoiler}
            >
                <separator />
            </container>
            """,
            pretext:
            """
            var id = 123;
            var color = Discord.Color.Blue;
            var spoiler = false;
            """
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    id: designer.GetValue<int>(0),
                    accentColor: designer.GetValue<global::Discord.Color>(1),
                    isSpoiler: designer.GetValue<bool>(2),
                    components: 
                    [
                        new global::Discord.SeparatorBuilder()
                    ]
                )
                """
            );
        }
    }
}