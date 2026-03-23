using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class FileTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyFile()
    {
        Graph(
            "<file />"
        );
        {
            var file = Component<FileComponentNode>();
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(file, file.Media));
            }
        }
    }

    [Fact]
    public void FileWithProperties()
    {
        Graph(
            """
            <file
                id='123'
                media='attachment://file.png'
                spoiler='false'
            />
            """
        );
        {
            Component<FileComponentNode>();

            Emits(
                """
                new global::Discord.FileComponentBuilder(
                    id: 123,
                    media: new global::Discord.UnfurledMediaItemProperties(
                        "attachment://file.png"
                    ),
                    isSpoiler: false
                )
                """
            );
        }
    }

    [Fact]
    public void FileWithChild()
    {
        Graph(
            """
            <file media='xyz'>
                <separator />
            </file>
            """
        );
        {
            var file = Component<FileComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.ComponentDoesntAllowChildren(file));
            }
        }
    }
}