using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class FileUploadTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyFileUpload()
    {
        Graph("<file-upload />");
        {
            var fileUpload = Component<FileUploadComponentNode>();
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(fileUpload, fileUpload.CustomId));
            }
        }
    }

    [Fact]
    public void BasicFileUpload()
    {
        Graph("<file-upload customId='foo' />");
        {
            Component<FileUploadComponentNode>();
            
            Emits(
                """
                new global::Discord.FileUploadComponentBuilder(
                    customId: "foo"
                )
                """
            );
        }
    }
    
    [Fact]
    public void FileUploadWithTypes()
    {
        Graph("<file-upload customId='foo' fileTypes=['.jpg', '.png'] />");
        {
            Component<FileUploadComponentNode>();

            Emits(
                """
                new global::Discord.FileUploadComponentBuilder(
                    customId: "foo",
                    fileTypes: 
                    [
                        ".jpg",
                        ".png"
                    ]
                )
                """
            );
        }
    }
}