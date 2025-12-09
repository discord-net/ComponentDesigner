using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;

namespace UnitTests.ComponentTests;

public sealed class FunctionalComponentTests : BaseComponentTest
{
    [Fact]
    public void ChildOfContainer()
    {
        Graph(
            """
            <container>
                <MyFunc />
            </container>
            """,
            additionalMethods:
            """
            public static MessageComponent MyFunc() => null!;
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<FunctionalComponentNode>();
            }
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        ..global::TestClass.MyFunc().Components.Select(x => x.ToBuilder())
                    ]
                }
                """
            );
            
            EOF();
        }
    }
}