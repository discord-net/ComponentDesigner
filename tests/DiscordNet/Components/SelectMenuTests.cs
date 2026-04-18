using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class SelectMenuTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyMenu()
    {
        Graph("<select-menu />");
        {
            var selectMenu = Component<SelectMenuComponentNode>();
            {
                AssertDiagnostic(Diagnostic.TypelessSelectMenu);
            }

            Emits(null);
        }
    }

    [Fact]
    public void EmptyStringSelectMenu()
    {
        Graph("<string-select />");
        {
            var selectMenu = Component<SelectMenuComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(selectMenu, selectMenu.CustomId));
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(selectMenu, selectMenu.Options));
            }
        }
    }

    [Fact]
    public void StringSelectMenuWithoutOptions()
    {
        Graph("<string-select customId='foo' />");
        {
            var selectMenu = Component<SelectMenuComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(selectMenu, selectMenu.Options));
            }
        }
    }

    [Fact]
    public void StringSelectMenuWithOptions()
    {
        Graph(
            """
            <string-select customId='foo'>
                <option
                    label='label1'
                    value='value1'
                    description='desc1'
                    emoji='😀'
                    default
                />
            </string-select>
            """
        );
        {
            Component<SelectMenuComponentNode>();
            {
                Component<SelectMenuOptionComponentNode>();
            }

            Emits(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.SelectMenu,
                    customId: "foo",
                    options: 
                    [
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "label1",
                            value: "value1",
                            description: "desc1",
                            emoji: global::Discord.Emoji.Parse("😀"),
                            isDefault: true
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void StringSelectMenuWithOptionsAsAttribute()
    {
        Graph(
            """
            <string-select 
                customId='foo'
                options=(
                    <>
                        <option
                            label='label1'
                            value='value1'
                            description='desc1'
                            emoji='😀'
                            default
                        />
                        <option
                            label='label2'
                            value='value2'
                            description='desc2'
                        />
                    </>
                )    
            />
            """
        );
        {
            Component<SelectMenuComponentNode>();
            {
                Component<SelectMenuOptionComponentNode>();
                Component<SelectMenuOptionComponentNode>();
            }

            Emits(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.SelectMenu,
                    customId: "foo",
                    options: 
                    [
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "label1",
                            value: "value1",
                            description: "desc1",
                            emoji: global::Discord.Emoji.Parse("😀"),
                            isDefault: true
                        ),
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "label2",
                            value: "value2",
                            description: "desc2"
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void StringSelectMenuWithInterpolatedOptions()
    {
        Graph(
            """
            <string-select customId='abc'>
                <option label='1' value='1'/>
                {opts}
                <option label='2' value='2'/>
            </string-select>
            """,
            pretext:
            "IEnumerable<SelectMenuOptionBuilder> opts = null!;"
        );
        {
            Component<SelectMenuComponentNode>();
            {
                Component<SelectMenuOptionComponentNode>();
                Component<InterpolationComponentNode>();
                Component<SelectMenuOptionComponentNode>();
            }

            Emits(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.SelectMenu,
                    customId: "abc",
                    options: 
                    [
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "1",
                            value: "1"
                        ),
                        ..designer.GetValue<global::System.Collections.Generic.IEnumerable<global::Discord.SelectMenuOptionBuilder>>(0),
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "2",
                            value: "2"
                        )
                    ]
                )
                """
            );
        }
    }
}