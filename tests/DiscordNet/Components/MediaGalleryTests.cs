using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class MediaGalleryTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyGallery()
    {
        Graph("<gallery />");
        {
            var gallery = Component<MediaGalleryComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.ComponentRequiresAtLeastOneChild(gallery)
                );
            }
        }
    }

    [Fact]
    public void GalleryWithTooManyItems()
    {
        Graph(
            """
            <gallery>
                <media-gallery-item url="1" />
                <media-gallery-item url="2" />
                <media-gallery-item url="3" />
                <media-gallery-item url="4" />
                <media-gallery-item url="5" />
                <media-gallery-item url="6" />
                <media-gallery-item url="7" />
                <media-gallery-item url="8" />
                <media-gallery-item url="9" />
                <media-gallery-item url="10" />
                <media-gallery-item url="11" />
                <media-gallery-item url="12" />
            </gallery>
            """
        );
        {
            var gallery = Component<MediaGalleryComponentNode>();
            {
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
            }

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.TooManyChildren(gallery, Validators.MEDIA_GALLERY_MAX_ITEMS)
                );
            }
        }
    }

    [Fact]
    public void BasicGallery()
    {
        Graph(
            """
            <gallery id='123'>
                <media-gallery-item media='1' />
                <media-gallery-item media='2' />
            </gallery>
            """
        );
        {
            Component<MediaGalleryComponentNode>();
            {
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
            }

            Emits(
                """
                new global::Discord.MediaGalleryBuilder(
                    id: 123,
                    items: 
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                "1"
                            )
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                "2"
                            )
                        )
                    ]
                )
                """
            );
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
            ContainerComponentNode container;
            var gallery = Component<MediaGalleryComponentNode>();
            {
                container = Component<ContainerComponentNode>();
            }

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.InvalidChildOfComponent(gallery, container)
                );

                AssertDiagnostic(
                    Diagnostic.ComponentRequiresAtLeastOneChild(container)
                );
            }
        }
    }

    [Fact]
    public void GalleryWithInterpolatedItems()
    {
        Graph(
            """
            <gallery>
                {item1}
                {item2}
            </gallery>
            """,
            pretext:
            """
            System.Uri item1 = new System.Uri("https://example.com/image1.png");
            Discord.MediaGalleryItemProperties item2 = new("https://example.com/image2.png");
            """
        );
        {
            Component<MediaGalleryComponentNode>();

            Emits(
                """
                new global::Discord.MediaGalleryBuilder(
                    items: 
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                syntax.GetValue<global::System.Uri>(0).ToString()
                            )
                        ),
                        syntax.GetValue<global::Discord.MediaGalleryItemProperties>(1)
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void GalleryWithItemsAndUriInterpolations()
    {
        Graph(
            """
            <gallery>
                <media-gallery-item url="https://example.com/image1.png"/>
                {url1}
                <media-gallery-item url="https://example.com/image3.png"/>
                {url2}
            </gallery>
            """,
            pretext:
            """
            System.Uri url1 = new System.Uri("https://example.com/image2.png");
            System.Uri url2 = new System.Uri("https://example.com/image4.png");
            """
        );
        {
            Component<MediaGalleryComponentNode>();
            {
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
            }

            Emits(
                """
                new global::Discord.MediaGalleryBuilder(
                    items: 
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                "https://example.com/image1.png"
                            )
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                syntax.GetValue<global::System.Uri>(0).ToString()
                            )
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                "https://example.com/image3.png"
                            )
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(
                                syntax.GetValue<global::System.Uri>(1).ToString()
                            )
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void GalleryWithTooManyMixedItems()
    {
        Graph(
            """
            <gallery>
                <media-gallery-item url="1" />
                <media-gallery-item url="2" />
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
            pretext:
            """
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
            var gallery = Component<MediaGalleryComponentNode>();
            {
                Component<MediaGalleryItemComponentNode>();
                Component<MediaGalleryItemComponentNode>();
            }

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.TooManyChildren(gallery, Validators.MEDIA_GALLERY_MAX_ITEMS)
                );
            }
        }
    }
}