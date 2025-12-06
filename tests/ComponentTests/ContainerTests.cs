using Discord.CX;
using Discord.CX.Nodes.Components;

namespace UnitTests.ComponentTests;

public sealed class ContainerTests : BaseComponentTest
{
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
    public void ContainerWithColor()
    {
        Graph(
            """
            <container color="blue" />
            """
        );
        {
            var container = Node<ContainerComponentNode>(out var graphNode);
            var color = graphNode.State.GetProperty(container.AccentColor);

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

            Diagnostic(Diagnostics.UnknownProperty.Id, message: "'unknown' is not a known property of 'container'");

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
                <button customId="abc" label="label"/>
            </container>
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<ContainerComponentNode>();
                Node<ButtonComponentNode>();
            }

            Validate();
            
            Diagnostic(
                Diagnostics.InvalidContainerChild.Id,
                message: "'container' is not a valid child component of 'container'"
            );
            Diagnostic(
                Diagnostics.InvalidContainerChild.Id,
                message: "'button' is not a valid child component of 'container'"
            );
            
            EOF();
        }
    }
}