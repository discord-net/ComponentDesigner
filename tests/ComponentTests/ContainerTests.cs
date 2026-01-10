using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class ContainerTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EnumerableChildren()
    {
        Graph(
            """
            <container>
                {a.Select((CXMessageComponent x) => CXMessageComponent.Empty)}
            </container>
            """,
            pretext:
            """
            List<CXMessageComponent> a = null!;
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<InterleavedComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        ..designer.GetValue<global::System.Collections.Generic.IEnumerable<global::Discord.CXMessageComponent>>(0).SelectMany(x => x.Builders)
                    ]
                }
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void ContainerWithInterpolatedChildren()
    {
        Graph(
            """
            <container>
                {a}
                <separator />
                {b}
                {c}
            </container>
            """,
            pretext:
            // values don't matter
            """
            CXMessageComponent a = null!;
            CXMessageComponent b = null!;
            CXMessageComponent c = null;
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<InterleavedComponentNode>();
                Node<SeparatorComponentNode>();
                Node<InterleavedComponentNode>();
                Node<InterleavedComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        ..designer.GetValue<global::Discord.CXMessageComponent>(0).Builders,
                        new global::Discord.SeparatorBuilder(),
                        ..designer.GetValue<global::Discord.CXMessageComponent>(1).Builders,
                        ..designer.GetValue<global::Discord.CXMessageComponent>(2).Builders
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void EmptyContainer()
    {
        Graph(
            """
            <container>

            </container>
            """
        );
        {
            Node<ContainerComponentNode>();

            Validate();

            Renders(
                """
                new global::Discord.ContainerBuilder()
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ContainerWithId()
    {
        Graph(
            """
            <container id="123" />
            """
        );
        {
            var container = Node<ContainerComponentNode>(out var graphNode);
            var id = graphNode.State.GetProperty(container.Id);

            Assert.True(id.IsSpecified);
            Assert.True(id.HasValue);

            Validate();

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Id = 123
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ContainerWithInterpolatedColor()
    {
        Graph(
            """
            <container color={color} />
            """,
            pretext: "Discord.Color color = default;"
        );
        {
            Node<ContainerComponentNode>();

            Validate();

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    AccentColor = designer.GetValue<global::Discord.Color>(0)
                }
                """
            );
        }
    }
    
    [Fact]
    public void ContainerWithNullableInterpolatedColor()
    {
        Graph(
            """
            <container color={color} />
            """,
            pretext: "Discord.Color? color = null;"
        );
        {
            Node<ContainerComponentNode>();

            Validate();
            
            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    AccentColor = designer.GetValue<global::Discord.Color?>(0)
                }
                """
            );
        }
    }

    [Fact]
    public void ContainerWithColor()
    {
        Graph(
            """
            <container color="blue" />
            """
        );
        {
            var container = Node<ContainerComponentNode>(out var graphNode);
            var color = graphNode.State!.GetProperty(container.AccentColor);

            Assert.True(color.IsSpecified);
            Assert.True(color.HasValue);

            Validate();

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    AccentColor = global::Discord.Color.Blue
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ContainerWithSpoiler()
    {
        Graph(
            """
            <container spoiler />
            """
        );
        {
            var container = Node<ContainerComponentNode>(out var graphNode);
            var spoiler = graphNode.State.GetProperty(container.Spoiler);

            Assert.True(spoiler.IsSpecified);

            Validate();

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    IsSpoiler = true
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ContainerWithUnknownProperty()
    {
        Graph(
            """
            <container spoiler unknown="abc" id={123} />
            """
        );
        {
            var container = Node<ContainerComponentNode>(out var graphNode);
            var spoiler = graphNode.State.GetProperty(container.Spoiler);

            Assert.True(spoiler.IsSpecified);

            Validate();

            Diagnostic(Diagnostics.UnknownProperty("unknown", "container"));

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Id = 123,
                    IsSpoiler = true
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ContainerWithValidChildren()
    {
        Graph(
            """
            <container id="123" color="red">
                <row>
                    <button customId="b1" label="label"/>
                </row>
                
                <text content="test1" />
                
                <section accessory=(<thumbnail url="abc" />)>
                    <text content="test2"/>
                </section>
                
                <gallery>
                    <media url="media1" />
                </gallery>
                
                <separator />
                
                <file url="file-url" />
            </container>
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<ActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                }

                Node<TextDisplayComponentNode>();

                Node<SectionComponentNode>();
                {
                    Node<ThumbnailComponentNode>();
                    Node<TextDisplayComponentNode>();
                }

                Node<MediaGalleryComponentNode>();
                {
                    Node<MediaGalleryItemComponentNode>();
                }

                Node<SeparatorComponentNode>();

                Node<FileComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Id = 123,
                    AccentColor = global::Discord.Color.Red,
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "label",
                                    customId: "b1"
                                )
                            ]
                        },
                        new global::Discord.TextDisplayBuilder(
                            content: "test1"
                        ),
                        new global::Discord.SectionBuilder(
                            accessory: new global::Discord.ThumbnailBuilder(
                                media: new global::Discord.UnfurledMediaItemProperties("abc")
                            ),
                            components:
                            [
                                new global::Discord.TextDisplayBuilder(
                                    content: "test2"
                                )
                            ]
                        ),
                        new global::Discord.MediaGalleryBuilder()
                        {
                            Items =
                            [
                                new global::Discord.MediaGalleryItemProperties(
                                    media: new global::Discord.UnfurledMediaItemProperties("media1")
                                )
                            ]
                        },
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.FileComponentBuilder(
                            media: new global::Discord.UnfurledMediaItemProperties("file-url")
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ContainerWithInvalidChildren()
    {
        Graph(
            """
            <container>
                <container />
            </container>
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<ContainerComponentNode>();
            }

            Validate();

            Diagnostic(
                Diagnostics.InvalidContainerChild("container")
            );
            EOF();
        }
    }
}