using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Json;

public sealed class ContainerTests(ITestOutputHelper output) : BaseJsonComponentTest(output)
{
    [Fact]
    public void ContainerWithProperties()
    {
        Graph(
            """
            <container
                id='123'
                accentColor='red'
                spoiler
            >
                <separator />
            </container>
            """
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                [
                    {
                        "type": 17,
                        "id": 123,
                        "components": [
                            {
                                "type": 14
                            }
                        ],
                        "spoiler": true
                    }
                ]
                """
            );
        }
    }
}