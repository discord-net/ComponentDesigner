using Discord.CX;
using Discord.CX.Nodes.Components;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class MediaGalleryTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyGallery()
    {
        Graph(
            """
            <gallery />
            """
        );
        {
            Node<MediaGalleryComponentNode>(out var galleryNode);

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MediaGalleryIsEmpty,
                galleryNode.State.Source
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithTooManyItems()
    {
        Graph(
            """
            <gallery>
                <item url="1" />
                <item url="2" />
                <item url="3" />
                <item url="4" />
                <item url="5" />
                <item url="6" />
                <item url="7" />
                <item url="8" />
                <item url="9" />
                <item url="10" />
                <item url="11" />
                <item url="12" />
            </gallery>
            """
        );
        {
            CXGraph.Node eleventh;
            CXGraph.Node twelfth;

            Node<MediaGalleryComponentNode>();
            {
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>(out eleventh);
                Node<MediaGalleryItemComponentNode>(out twelfth);
            }

            Validate(hasErrors: true);

            var errorSpan = TextSpan.FromBounds(
                eleventh.State.Source.Span.Start,
                twelfth.State.Source.Span.End
            );

            Diagnostic(
                Diagnostics.TooManyItemsInMediaGallery,
                errorSpan
            );

            EOF();
        }
    }

    [Fact]
    public void BasicGallery()
    {
        Graph(
            """
            <gallery id={123}>
                <item url="1" />
                <item url="2" />
            </gallery>
            """
        );
        {
            Node<MediaGalleryComponentNode>();
            {
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.MediaGalleryBuilder()
                {
                    Id = 123,
                    Items =
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties("1")
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties("2")
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithInvalidChild()
    {
        Graph(
            """
            <gallery>
                <container />
            </gallery>
            """
        );
        {
            CXGraph.Node containerNode;

            Node<MediaGalleryComponentNode>();
            {
                Node<ContainerComponentNode>(out containerNode);
            }

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.InvalidMediaGalleryChild("container"),
                containerNode.State.Source
            );
            
            Diagnostic(Diagnostics.MediaGalleryIsEmpty.Id);
            
            EOF();
        }
    }

    [Fact]
    public void GalleryWithUriInterpolation()
    {
        Graph(
            """
            <gallery>
                {url1}
                {url2}
            </gallery>
            """,
            pretext: """
            System.Uri url1 = new System.Uri("https://example.com/image1.png");
            System.Uri url2 = new System.Uri("https://example.com/image2.png");
            """
        );
        {
            Node<MediaGalleryComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.MediaGalleryBuilder()
                {
                    Items =
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(0))
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(1))
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }
}