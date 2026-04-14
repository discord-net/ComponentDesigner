using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class ActionRowTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyRow()
    {
        Graph(
            "<row />"
        );
        {
            var row = Component<ActionRowComponentNode>();
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.ComponentRequiresAtLeastOneChild(row));
            }
        }
    }

    [Fact]
    public void RowIdWithButtons()
    {
        Graph(
            """
            <row id='123'>
                <button customId='foo' label='foo' />
            </row>
            """
        );
        {
            Component<ActionRowComponentNode>();
            {
                Component<ButtonComponentNode>();
            }
            
            Emits(
                """
                new global::Discord.ActionRowBuilder(
                    id: 123,
                    components: 
                    [
                        new global::Discord.ButtonBuilder(
                            label: "foo",
                            customId: "foo"
                        )
                    ]
                )
                """
                );
        }
    }

    [Fact]
    public void TooManyButtons()
    {
        Graph(
            """
            <row>
                <button customId='foo' label='foo' />
                <button customId='foo' label='foo' />
                <button customId='foo' label='foo' />
                <button customId='foo' label='foo' />
                <button customId='foo' label='foo' />
                <button customId='foo' label='foo' />
            </row>
            """
        );
        {
            var row =Component<ActionRowComponentNode>();
            {
                Component<ButtonComponentNode>();
                Component<ButtonComponentNode>();
                Component<ButtonComponentNode>();
                Component<ButtonComponentNode>();
                Component<ButtonComponentNode>();
                Component<ButtonComponentNode>();
            }
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.TooManyChildren(row, 5));
            }
        }
    }

    [Fact]
    public void MixOfSelectMenusAndButtons()
    {
        Graph(
            """
            <row>
                <button url="url-1" label="label-1" />
                <select type="user" customId="abc" />
                <button url="url-2" label="label-2" />
            </row>
            """
        );
        {
            SelectMenuComponentNode menu;
            var row = Component<ActionRowComponentNode>();
            {
                Component<ButtonComponentNode>();
                menu = Component<SelectMenuComponentNode>();
                Component<ButtonComponentNode>();
            }
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.InvalidChildOfComponent(row, menu));
            }
        }
    }

    [Fact]
    public void RowWithSelectMenu()
    {
        Graph(
            """
            <row>
                <user-select customId='foo' />
            </row>
            """
        );
        {
            Component<ActionRowComponentNode>();
            {
                Component<SelectMenuComponentNode>();
            }

            Emits(
                """
                new global::Discord.ActionRowBuilder(
                    components: 
                    [
                        new global::Discord.SelectMenuBuilder(
                            type: global::Discord.ComponentType.UserSelect,
                            customId: "foo"
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void RowWithInvalidChild()
    {
        Graph(
            """
            <row>
                <button customId='foo' label='foo'/>
                <separator />
            </row>
            """
        );
        {
            SeparatorComponentNode separator;
            var row = Component<ActionRowComponentNode>();
            {
                Component<ButtonComponentNode>();
                separator = Component<SeparatorComponentNode>();
            }
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.InvalidChildOfComponent(row, separator));
            }
        }
    }
}