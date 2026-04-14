using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Json;

public class TextDisplayTests(ITestOutputHelper output) : BaseJsonComponentTest(output)
{
    [Fact]
    public void ContentInChildren()
    {
        Graph(
            """
            <text>
                Hello, world!
            </text>
            """
        );
        {
            Component<TextDisplayComponentNode>();
            {
                Component<TextControlNode>();
            }

            Emits(
                """
                [
                    {
                        "type": 10,
                        "content": "Hello, world!"
                    }
                ]
                """
            );
        }
    }
}