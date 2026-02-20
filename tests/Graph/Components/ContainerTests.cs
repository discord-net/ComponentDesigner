using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class ContainerTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyContainer()
    {
        Graph(
            "<container />"
        );
        {
            var container = Component<ContainerComponentNode>(out var containerNode);
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.ComponentRequiresAtLeastOneChild(container), containerNode.State.TextSpan);
            }
        }
    }

    [Fact]
    public void ContainerWithProperties()
    {
        Graph(
            """
            <container
                id='123'
                accentColor='red'
                spoiler
                unknown
            >
                <separator />
            </container>
            """
        );
        {
            var container = Component<ContainerComponentNode>(out var containerNode);
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    id: 123,
                    accentColor: global::Discord.Color.Red,
                    isSpoiler: true,
                    components: 
                    [
                        new global::Discord.SeparatorBuilder()
                    ]
                )
                """
            );
            {
                AssertDiagnostic(Diagnostic.UnknownPropertyOfComponent(container, "unknown"));
            }
        }
    }

    [Fact]
    public void ContainerWithInterpolatedProperties()
    {
        Graph(
            """
            <container
                id={id}
                accentColor={color}
                spoiler={spoiler}
            >
                <separator />
            </container>
            """,
            pretext:
            """
            var id = 123;
            var color = Discord.Color.Blue;
            var spoiler = false;
            """
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    id: designer.GetValue<int>(0),
                    accentColor: designer.GetValue<global::Discord.Color>(1),
                    isSpoiler: designer.GetValue<bool>(2),
                    components: 
                    [
                        new global::Discord.SeparatorBuilder()
                    ]
                )
                """
            );
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
            Component<ContainerComponentNode>();
            {
                Component<ActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                }

                Component<TextDisplayComponentNode>();

                Component<SectionComponentNode>();
                {
                    Component<ThumbnailComponentNode>();
                    Component<TextDisplayComponentNode>();
                }

                Component<MediaGalleryComponentNode>();
                {
                    Component<MediaGalleryItemComponentNode>();
                }

                Component<SeparatorComponentNode>();

                Component<FileComponentNode>();
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    id: 123,
                    accentColor: global::Discord.Color.Red,
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    label: "label",
                                    customId: "b1"
                                )
                            ]
                        ),
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
                        new global::Discord.MediaGalleryBuilder(
                            items: new global::Discord.MediaGalleryItemProperties(
                                media: new global::Discord.UnfurledMediaItemProperties("media1")
                            )
                        ),
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.FileComponentBuilder(
                            media: new global::Discord.UnfurledMediaItemProperties("file-url")
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void ContainerWithInvalidChild()
    {
        Graph(
            """
            <container>
                <container />
            </container>
            """
        );
        {
            ContainerComponentNode child;
            var parent = Component<ContainerComponentNode>();
            {
                child = Component<ContainerComponentNode>();
            }
            
            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic
                        .InvalidChildOfComponent(parent, child)
                );
            }
        }
    }
}