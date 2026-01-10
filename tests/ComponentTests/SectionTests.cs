using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class SectionTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void WithButtonAccessory()
    {
        Graph(
            """
            <section
                accessory=(
                    <button customId='b1' label='b1' />
                )
            >
                <text>Foo</text>
            </section>
            """
        );
        {
            Node<SectionComponentNode>();
            {
                Node<ButtonComponentNode>();
                Node<TextDisplayComponentNode>();
            }
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ButtonBuilder(
                        style: global::Discord.ButtonStyle.Primary,
                        label: "b1",
                        customId: "b1"
                    ),
                    components:
                    [
                        new global::Discord.TextDisplayBuilder(
                            content: "Foo"
                        )
                    ]
                )
                """
            );
        }
    }
    
    [Fact]
    public void EmptySection()
    {
        Graph(
            """
            <section />
            """
        );
        {
            Node<SectionComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.EmptySection.Id
            );
            
            EOF();
        }
    }

    [Fact]
    public void SectionWithInlineAccessory()
    {
        Graph(
            """
            <section accessory=(<thumbnail url="abc" />)>
                <text>Hello</text>
            </section>
            """
        );
        {
            Node<SectionComponentNode>();
            {
                Node<ThumbnailComponentNode>();
                Node<TextDisplayComponentNode>();
            }
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ThumbnailBuilder(
                        media: new global::Discord.UnfurledMediaItemProperties("abc")
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
            
            EOF();
        }
    }

    [Fact]
    public void SectionWithChildAccessory()
    {
        Graph(
            """
            <section>
                <accessory>
                    <thumbnail url="abc" />
                </accessory>
                <text>Hello</text>
            </section>
            """
        );
        {
            Node<SectionComponentNode>();
            {
                Node<AccessoryComponentNode>();
                {
                    Node<ThumbnailComponentNode>();
                }
                Node<TextDisplayComponentNode>();
            }
            
            Validate(hasErrors: false);
            
            Renders(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ThumbnailBuilder(
                        media: new global::Discord.UnfurledMediaItemProperties("abc")
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
            
            EOF();
        }
    }
}