using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class SectionTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptySection()
    {
        Graph(
            "<section />"
        );
        {
            var section = Component<SectionComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.ComponentRequiresAtLeastOneChild(section)
                );

                AssertDiagnostic(
                    Diagnostic.RequiredPropertyNotSpecified(section, section.Accessory)
                );
            }
        }
    }

    [Fact]
    public void SectionWithInlineAccessory()
    {
        Graph(
            """
            <section accessory=(<thumbnail url="foo" />)>
                <text-display>Hello</text-display>
            </section>
            """
        );
        {
            Component<SectionComponentNode>();
            {
                Component<ThumbnailComponentNode>();
                Component<TextDisplayComponentNode>();
                {
                    Component<TextControlNode>();
                }
            }

            Emits(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ThumbnailBuilder(
                        media: new global::Discord.UnfurledMediaItemProperties(
                            "foo"
                        )
                    ),
                    components: 
                    [
                        new global::Discord.TextDisplayBuilder(
                            content: "Hello"
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void SectionWithInvalidInlineAccessory()
    {
        Graph(
            """
            <section accessory=(<container />)>
                <text-display>Hello</text-display>
            </section>
            """
        );
        {
            ContainerComponentNode container;
            Component<SectionComponentNode>();
            {
                container = Component<ContainerComponentNode>();
                Component<TextDisplayComponentNode>();
                {
                    Component<TextControlNode>();
                }
            }

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.InvalidAccessoryComponentOfSection(container)
                );

                AssertDiagnostic(
                    Diagnostic.ComponentRequiresAtLeastOneChild(container)
                );
            }
        }
    }

    [Fact]
    public void SectionWithChildAccessory()
    {
        Graph(
            """
            <section>
                <accessory>
                    <thumbnail url="foo"/>
                </accessory>
                <text-display>
                    Hello
                </text-display>
            </section>
            """
        );
        {
            Component<SectionComponentNode>();
            {
                Component<ThumbnailComponentNode>();
                Component<TextDisplayComponentNode>();
                {
                    Component<TextControlNode>();
                }
            }

            Emits(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ThumbnailBuilder(
                        media: new global::Discord.UnfurledMediaItemProperties(
                            "foo"
                        )
                    ),
                    components: 
                    [
                        new global::Discord.TextDisplayBuilder(
                            content: "Hello"
                        )
                    ]
                )
                """
            );
        }
    }
}