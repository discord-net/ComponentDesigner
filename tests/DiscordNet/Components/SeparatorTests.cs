using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class SeparatorTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptySeparator()
    {
        Graph(
            "<separator />"
        );
        {
            Component<SeparatorComponentNode>();

            Emits("new global::Discord.SeparatorBuilder()");
        }
    }

    [Fact]
    public void SeparatorWithProperties()
    {
        Graph(
            """
            <separator
                id='123'
                divider='false'
                spacing='large'
            />
            """
        );
        {
            Component<SeparatorComponentNode>();

            Emits(
                """
                new global::Discord.SeparatorBuilder(
                    id: 123,
                    spacing: global::Discord.SeparatorSpacingSize.Large,
                    isDivider: false
                )
                """
            );
        }
    }

    [Fact]
    public void SeparatorWithChild()
    {
        Graph(
            """
            <separator>
                <separator />
            </separator>
            """
        );
        {
            var separator = Component<SeparatorComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.ComponentDoesntAllowChildren(separator));
            }
        }
    }
}