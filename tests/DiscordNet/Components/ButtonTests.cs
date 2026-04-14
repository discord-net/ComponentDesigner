using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public sealed class ButtonTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void EmptyButton()
    {
        Graph("<button />");
        {
            var button = Component<ButtonComponentNode>(out var buttonNode);

            Emits(null);
            {
                AssertDiagnostic(Diagnostic.RequiredPropertyNotSpecified(button, button.CustomId));

                AssertDiagnostic(Diagnostic.MissingOneOfProperties(
                    button,
                    buttonNode.State.GetPropertyValue(button.Label),
                    buttonNode.State.GetPropertyValue(button.Emoji)
                ));
            }
        }
    }

    [Fact]
    public void BasicButton()
    {
        Graph(
            """
            <button
                id='1'
                customId='my-button'
                style='secondary'
                label='my label'
            />
            """
        );
        {
            Component<ButtonComponentNode>();

            Emits(
                """
                new global::Discord.ButtonBuilder(
                    id: 1,
                    style: global::Discord.ButtonStyle.Secondary,
                    label: "my label",
                    customId: "my-button"
                )
                """
            );
        }
    }

    [Fact]
    public void BasicButtonWithForbiddenProperties()
    {
        Graph(
            """
            <button 
                id={123}
                style="primary"
                label="label1"
                emoji="😀"
                customId="button1"
                sku="1"
                url="abc"
                disabled='false'
            />
            """
        );
        {
            var button = Component<ButtonComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Default, button.SkuId)
                );

                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Default, button.Url)
                );
            }
        }
    }

    [Fact]
    public void LinkButtonWithForbiddenProperties()
    {
        Graph(
            """
            <button 
                id={123}
                style="link"
                label="label1"
                emoji="😀"
                customId="button1"
                sku="1"
                url="abc"
                disabled='false'
            />
            """
        );
        {
            var button = Component<ButtonComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Link, button.CustomId)
                );

                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Link, button.SkuId)
                );
            }
        }
    }

    [Fact]
    public void PremiumButtonWithForbiddenProperties()
    {
        Graph(
            """
            <button 
                id={123}
                style="premium"
                label="label1"
                emoji="😀"
                customId="button1"
                sku="1"
                url="abc"
                disabled='false'
            />
            """
        );
        {
            var button = Component<ButtonComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Premium, button.CustomId)
                );

                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Premium, button.Url)
                );

                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Premium, button.Label)
                );

                AssertDiagnostic(
                    Diagnostic.ButtonPropertyNotAllowed(ButtonKind.Premium, button.Emoji)
                );
            }
        }
    }

    [Fact]
    public void EmptyLinkButton()
    {
        Graph(
            "<link-button />"
        );
        {
            var button = Component<ButtonComponentNode, ButtonState>(out var buttonNode, out var state);

            Assert.Equal(ButtonKind.Link, state.InferredKind);

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.RequiredPropertyNotSpecified(button, button.Url)
                );

                AssertDiagnostic(
                    Diagnostic.MissingOneOfProperties(
                        button,
                        button.Label,
                        button.Emoji
                    )
                );
            }
        }
    }

    [Fact]
    public void EmptyPremiumButton()
    {
        Graph(
            """
            <button style="premium"/>
            """
        );
        {
            var button = Component<ButtonComponentNode, ButtonState>(out var buttonNode, out var state);

            Assert.Equal(ButtonKind.Premium, state.InferredKind);

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.RequiredPropertyNotSpecified(button, button.SkuId)
                );
            }
        }
    }

    [Fact]
    public void ButtonLabelAsChild()
    {
        Graph(
            """
            <button customId="abc">
                My Label
            </button>
            """
        );
        {
            Component<ButtonComponentNode>();

            Emits(
                """
                new global::Discord.ButtonBuilder(
                    label: "My Label",
                    customId: "abc"
                )
                """
            );
        }
    }

    [Fact]
    public void LabelIsTooLong()
    {
        Graph(
            """
            <button 
                customId="button1"
                label="This label is too long, the max label length is 80 characters and this should report a diagnostic"
            />
            """
        );
        {
            var button = Component<ButtonComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.StringOutOfRange(button.Label, 97, upper: Validators.BUTTON_LABEL_MAX_LENGTH)
                );
            }
        }
    }

    [Fact]
    public void CustomIdIsTooLong()
    {
        Graph(
            """
            <button 
                label="button"
                customId="This custom id is too long, the max custom id length is 100 characters and this should report a diagnostic"
            />
            """
        );
        {
            var button = Component<ButtonComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.StringOutOfRange(
                        button.CustomId,
                        106,
                        lower: Validators.BUTTON_CUSTOM_ID_MIN_LENGTH,
                        upper: Validators.BUTTON_CUSTOM_ID_MAX_LENGTH
                    )
                );
            }
        }
    }

    [Fact]
    public void UnknownButtonStyle()
    {
        Graph(
            """
            <button 
                style="invalid"
                customId="button"
                label="button"
            />
            """
        );
        {
            Component<ButtonComponentNode>();
            
            Emits(null);
            {
                AssertDiagnostic(
                    Diagnostic.NotAValidEnumVariant("Discord.ButtonStyle", "invalid")
                );
            }
        }
    }
}