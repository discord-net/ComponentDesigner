using Discord;
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
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(0).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(1).ToString())
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
                {url1}
                <item url="https://example.com/image3.png" />
                {url2}
            </gallery>
            """,
            pretext: """
                     System.Uri url1 = new System.Uri("https://example.com/image2.png");
                     System.Uri url2 = new System.Uri("https://example.com/image4.png");
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
            Renders(
                """
                new global::Discord.MediaGalleryBuilder()
                {
                    Items =
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties("https://example.com/image1.png")
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(0).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties("https://example.com/image3.png")
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(1).ToString())
                        )
                    ]
                }
                """
            );

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
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(0).ToString())
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

            Renders(
                """
                new global::Discord.MediaGalleryBuilder()
                {
                    Items =
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(0).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(1).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(2).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(3).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(4).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(5).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(6).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(7).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(8).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(9).ToString())
                        )
                    ]
                }
                """
            );

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

    [Fact]
    public void GalleryWithStringInterpolation()
    {
        Graph(
            """
            <gallery>
                {url1}
                {url2}
            </gallery>
            """,
            pretext: """
                     string url1 = "https://example.com/image1.png";
                     string url2 = "https://example.com/image2.png";
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
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<string>(0))
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<string>(1))
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithUnfurledMediaItemInterpolation()
    {
        Graph(
            """
            <gallery>
                {item}
            </gallery>
            """,
            pretext: """
                     Discord.UnfurledMediaItemProperties item = new Discord.UnfurledMediaItemProperties("https://example.com/image.png");
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
                            media: designer.GetValue<global::Discord.UnfurledMediaItemProperties>(0)
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithEnumerableOfUris()
    {
        Graph(
            """
            <gallery>
                {urls}
            </gallery>
            """,
            pretext: """
                     System.Collections.Generic.List<System.Uri> urls = new System.Collections.Generic.List<System.Uri> { 
                         new System.Uri("https://example.com/1.png"),
                         new System.Uri("https://example.com/2.png")
                     };
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
                        ..designer.GetValue<global::System.Collections.Generic.List<global::System.Uri>>(0).Select(x => new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(x.ToString())
                        ))
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithEnumerableOfStrings()
    {
        Graph(
            """
            <gallery>
                {urls}
            </gallery>
            """,
            pretext: """
                     System.Collections.Generic.List<string> urls = new System.Collections.Generic.List<string> { 
                         "https://example.com/1.png",
                         "https://example.com/2.png"
                     };
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
                        ..designer.GetValue<global::System.Collections.Generic.List<string>>(0).Select(x => new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(x)
                        ))
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithEnumerableOfUnfurledMediaItems()
    {
        Graph(
            """
            <gallery>
                {items}
            </gallery>
            """,
            pretext: """
                     System.Collections.Generic.List<Discord.UnfurledMediaItemProperties> items = new System.Collections.Generic.List<Discord.UnfurledMediaItemProperties> { 
                         new Discord.UnfurledMediaItemProperties("https://example.com/1.png"),
                         new Discord.UnfurledMediaItemProperties("https://example.com/2.png")
                     };
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
                        ..designer.GetValue<global::System.Collections.Generic.List<global::Discord.UnfurledMediaItemProperties>>(0).Select(x => new global::Discord.MediaGalleryItemProperties(
                            media: x
                        ))
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryMixingAllTypes()
    {
        Graph(
            """
            <gallery>
                <item url="https://static.example.com/1.png" />
                {uriValue}
                {stringValue}
                {unfurledValue}
            </gallery>
            """,
            pretext: """
                     System.Uri uriValue = new System.Uri("https://example.com/2.png");
                     string stringValue = "https://example.com/3.png";
                     Discord.UnfurledMediaItemProperties unfurledValue = new Discord.UnfurledMediaItemProperties("https://example.com/4.png");
                     """
        );
        {
            Node<MediaGalleryComponentNode>();
            {
                Node<MediaGalleryItemComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.MediaGalleryBuilder()
                {
                    Items =
                    [
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties("https://static.example.com/1.png")
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<global::System.Uri>(0).ToString())
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(designer.GetValue<string>(1))
                        ),
                        new global::Discord.MediaGalleryItemProperties(
                            media: designer.GetValue<global::Discord.UnfurledMediaItemProperties>(2)
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithIEnumerableOfUris()
    {
        Graph(
            """
            <gallery>
                {urls}
            </gallery>
            """,
            pretext: """
                     System.Collections.Generic.IEnumerable<System.Uri> urls = System.Linq.Enumerable.Empty<System.Uri>();
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
                        ..designer.GetValue<global::System.Collections.Generic.IEnumerable<global::System.Uri>>(0).Select(x => new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(x.ToString())
                        ))
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithIEnumerableOfStrings()
    {
        Graph(
            """
            <gallery>
                {urls}
            </gallery>
            """,
            pretext: """
                     System.Collections.Generic.IEnumerable<string> urls = System.Linq.Enumerable.Empty<string>();
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
                        ..designer.GetValue<global::System.Collections.Generic.IEnumerable<string>>(0).Select(x => new global::Discord.MediaGalleryItemProperties(
                            media: new global::Discord.UnfurledMediaItemProperties(x)
                        ))
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void GalleryWithIEnumerableOfUnfurledMediaItems()
    {
        Graph(
            """
            <gallery>
                {items}
            </gallery>
            """,
            pretext: """
                     System.Collections.Generic.IEnumerable<Discord.UnfurledMediaItemProperties> items = System.Linq.Enumerable.Empty<Discord.UnfurledMediaItemProperties>();
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
                        ..designer.GetValue<global::System.Collections.Generic.IEnumerable<global::Discord.UnfurledMediaItemProperties>>(0).Select(x => new global::Discord.MediaGalleryItemProperties(
                            media: x
                        ))
                    ]
                }
                """
            );

            EOF();
        }
    }
}