using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class AutoActionRowTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void MixOfButtonsAndSelectMenus()
    {
        Graph(
            """
            <container>
                <user-select customId='1' />
                <button customId='2' label='2'/>
                <button customId="3" label="3" />
                <button customId="4" label="4" />
                <role-select customId='5'/>
                <role-select customId='6'/>
                <button customId="7" label="7" />
                <button customId="8" label="8" />
            </container>
            """,
            options: new(
                AllowAutoRows: true
            )
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }

                Component<AutoActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                }

                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }

                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }

                Component<AutoActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                }
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.UserSelect,
                                    customId: "1"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "2",
                                    customId: "2"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "3",
                                    customId: "3"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "4",
                                    customId: "4"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "5"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "6"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "7",
                                    customId: "7"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "8",
                                    customId: "8"
                                )
                            ]
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void SingleSelectMenu()
    {
        Graph(
            """
            <container>
                <role-select customId="abc" />
            </container>
            """,
            options: new(AllowAutoRows: true)
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "abc"
                                )
                            ]
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void ManySelectMenus()
    {
        Graph(
            """
            <container>
                <role-select customId='1' />
                <user-select customId='2' />
                <channel-select customId='3' />
            </container>
            """,
            options: new(AllowAutoRows: true)
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }
                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }
                Component<AutoActionRowComponentNode>();
                {
                    Component<SelectMenuComponentNode>();
                }
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "1"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.UserSelect,
                                    customId: "2"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.ChannelSelect,
                                    customId: "3"
                                )
                            ]
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void SingleButton()
    {
        Graph(
            """
            <container>
                <button 
                    customId='abc'
                    label='abc'
                /> 
            </container>
            """,
            options: new(AllowAutoRows: true)
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<AutoActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                }
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "abc",
                                    customId: "abc"
                                )
                            ]
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void MultipleButtonsInSingleRow()
    {
        Graph(
            """
            <container>
                <button 
                    customId="1"
                    label="1"
                />
                <button 
                    customId="2"
                    label="2"
                />
                <button 
                    customId="3"
                    label="3"
                />
            </container>
            """,
            options: new(AllowAutoRows: true)
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<AutoActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                }
            }

            Emits(
                """
                new global::Discord.ContainerBuilder(
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "1",
                                    customId: "1"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "2",
                                    customId: "2"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "3",
                                    customId: "3"
                                )
                            ]
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void MultipleButtonAutoRows()
    {
        Graph(
            """
            <container>
                <button 
                    customId="1"
                    label="1"
                />
                <button 
                    customId="2"
                    label="2"
                />
                <button 
                    customId="3"
                    label="3"
                />
                <button 
                    customId="4"
                    label="4"
                />
                <button 
                    customId="5"
                    label="5"
                />
                <button 
                    customId="6"
                    label="6"
                />
                <button 
                    customId="7"
                    label="7"
                />
            </container>
            """,
            options: new(AllowAutoRows: true)
        );
        {
            Component<ContainerComponentNode>();
            {
                Component<AutoActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                }
                
                Component<AutoActionRowComponentNode>();
                {
                    Component<ButtonComponentNode>();
                    Component<ButtonComponentNode>();
                }
            }
            
            Emits(
                """
                new global::Discord.ContainerBuilder(
                    components: 
                    [
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "1",
                                    customId: "1"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "2",
                                    customId: "2"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "3",
                                    customId: "3"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "4",
                                    customId: "4"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "5",
                                    customId: "5"
                                )
                            ]
                        ),
                        new global::Discord.ActionRowBuilder(
                            components: 
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "6",
                                    customId: "6"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "7",
                                    customId: "7"
                                )
                            ]
                        )
                    ]
                )
                """
            );
        }
    }
}