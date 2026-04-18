using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class LabelTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyLabel()
    {
        Graph("<label/>");
        {
            var label = Component<LabelComponentNode>();
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.ComponentRequiresOneChild(label));
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(label, label.Label));
            }
        }
    }

    [Fact]
    public void LabelTextInChildren()
    {
        Graph(
            """
            <label>
                Text
                <file-upload customId='x'/>
            </label>
            """
        );
        {
            Component<LabelComponentNode>();
            {
                Component<FileUploadComponentNode>();
            }

            Emits(
                """
                new global::Discord.LabelBuilder(
                    label: "Text",
                    component: new global::Discord.FileUploadComponentBuilder(
                        customId: "x"
                    )
                )
                """
            );
        }
    }
}