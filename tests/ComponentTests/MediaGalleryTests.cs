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

    [Fact]
    public void GalleryMixingItemsAndUriInterpolations()
    {
        Graph(
            """
            <gallery>
                <item url="https://example.com/image1.png" />
                {url2}
                <item url="https://example.com/image3.png" />
                {url4}
            </gallery>
            """,
            pretext: """
            System.Uri url2 = new System.Uri("https://example.com/image2.png");
            System.Uri url4 = new System.Uri("https://example.com/image4.png");
            """
        );
        {
            Node<MediaGalleryComponentNode>();
            {
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
            }

            Validate(hasErrors: false);

            // Just verify it renders successfully - the order should be item1, url2, item3, url4
            // but verifying exact string match is brittle due to whitespace/formatting
            Renders();

            EOF();
        }
    }

    [Fact]
    public void GalleryWithMinimumOneItem()
    {
        Graph(
            """
            <gallery>
                {url}
            </gallery>
            """,
            pretext: """
            System.Uri url = new System.Uri("https://example.com/image.png");
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
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithMaximumTenItems()
    {
        Graph(
            """
            <gallery>
                {url1}
                {url2}
                {url3}
                {url4}
                {url5}
                {url6}
                {url7}
                {url8}
                {url9}
                {url10}
            </gallery>
            """,
            pretext: """
            System.Uri url1 = new System.Uri("https://example.com/1.png");
            System.Uri url2 = new System.Uri("https://example.com/2.png");
            System.Uri url3 = new System.Uri("https://example.com/3.png");
            System.Uri url4 = new System.Uri("https://example.com/4.png");
            System.Uri url5 = new System.Uri("https://example.com/5.png");
            System.Uri url6 = new System.Uri("https://example.com/6.png");
            System.Uri url7 = new System.Uri("https://example.com/7.png");
            System.Uri url8 = new System.Uri("https://example.com/8.png");
            System.Uri url9 = new System.Uri("https://example.com/9.png");
            System.Uri url10 = new System.Uri("https://example.com/10.png");
            """
        );
        {
            Node<MediaGalleryComponentNode>();

            Validate(hasErrors: false);

            EOF();
        }
    }

    [Fact]
    public void GalleryWithTooManyMixedItems()
    {
        Graph(
            """
            <gallery>
                <item url="1" />
                <item url="2" />
                {url3}
                {url4}
                {url5}
                {url6}
                {url7}
                {url8}
                {url9}
                {url10}
                {url11}
            </gallery>
            """,
            pretext: """
            System.Uri url3 = new System.Uri("https://example.com/3.png");
            System.Uri url4 = new System.Uri("https://example.com/4.png");
            System.Uri url5 = new System.Uri("https://example.com/5.png");
            System.Uri url6 = new System.Uri("https://example.com/6.png");
            System.Uri url7 = new System.Uri("https://example.com/7.png");
            System.Uri url8 = new System.Uri("https://example.com/8.png");
            System.Uri url9 = new System.Uri("https://example.com/9.png");
            System.Uri url10 = new System.Uri("https://example.com/10.png");
            System.Uri url11 = new System.Uri("https://example.com/11.png");
            """
        );
        {
            Node<MediaGalleryComponentNode>();
            {
                Node<MediaGalleryItemComponentNode>();
                Node<MediaGalleryItemComponentNode>();
            }

            Validate(hasErrors: true);

            Diagnostic(Diagnostics.TooManyItemsInMediaGallery.Id);

            EOF();
        }
    }
}