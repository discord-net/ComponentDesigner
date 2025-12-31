using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class ButtonTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyButton()
    {
        Graph(
            """
            <button />
            """
        );
        {
            Node<ButtonComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty(owner: "button", property: "customId")
            );
            
            Diagnostic(
                Diagnostics.MissingRequiredProperty(owner: "button", property: "label' or 'emoji")
            );
            
            EOF();
        }
    }

    [Fact]
    public void BasicButton()
    {
        Graph(
            """
            <button
                id='1'
                customId="button1"
                style="secondary"
                label="label1"
            />
            """
        );
        {
            var button = Node<ButtonComponentNode>(out var buttonNode);

            var id = buttonNode.State!.GetProperty(button.Id);
            var customId = buttonNode.State.GetProperty(button.CustomId);
            var style = buttonNode.State.GetProperty(button.Style);
            var label = buttonNode.State.GetProperty(button.Label);
            
            Assert.True(id is { IsSpecified: true, HasValue: true });
            Assert.True(customId is { IsSpecified: true, HasValue: true });
            Assert.True(style is { IsSpecified: true, HasValue: true });
            Assert.True(label is { IsSpecified: true, HasValue: true });
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ButtonBuilder(
                    style: global::Discord.ButtonStyle.Secondary,
                    id: 1,
                    label: "label1",
                    customId: "button1"
                )
                """
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);

            var sku = buttonNode.State!.GetProperty(button.SkuId);
            var url = buttonNode.State.GetProperty(button.Url);

            Assert.NotNull(sku.Attribute);
            Assert.NotNull(url.Attribute);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.PropertyNotAllowed(owner: "button", property: "sku"),
                span: sku.Attribute.Span
            );
            
            Diagnostic(
                Diagnostics.PropertyNotAllowed(owner: "button", property: "url"),
                url.Attribute
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);

            var customId = buttonNode.State.GetProperty(button.CustomId);
            var sku = buttonNode.State.GetProperty(button.SkuId);
            
            Assert.NotNull(customId.Attribute);
            Assert.NotNull(sku.Attribute);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.PropertyNotAllowed(owner: "button", property: "customId"),
                customId.Attribute
            );
            
            Diagnostic(
                Diagnostics.PropertyNotAllowed(owner: "button", "sku"),
                sku.Attribute
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);

            var customId = buttonNode.State.GetProperty(button.CustomId);
            var label = buttonNode.State.GetProperty(button.Label);
            var url = buttonNode.State.GetProperty(button.Url);
            var emoji = buttonNode.State.GetProperty(button.Emoji);
            
            Assert.NotNull(customId.Attribute);
            Assert.NotNull(label.Attribute);
            Assert.NotNull(url.Attribute);
            Assert.NotNull(emoji.Attribute);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.PropertyNotAllowed("button", "customId"),
                customId.Attribute
            );
            
           
            Diagnostic(
                Diagnostics.PropertyNotAllowed("button", "url"),
                url.Attribute
            );
            
            Diagnostic(
                Diagnostics.PropertyNotAllowed("button", "label"),
                label.Attribute
            );
            
            Diagnostic(
                Diagnostics.PropertyNotAllowed("button", "emoji"),
                emoji.Attribute
            );
            
            EOF();
        }
    }

    [Fact]
    public void EmptyLinkButton()
    {
        Graph(
            """
            <button style="link"/>
            """
        );
        {
            var button = Node<ButtonComponentNode>(out var buttonNode);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty("button", "url")
            );
            
            Diagnostic(
                Diagnostics.MissingRequiredProperty("button", "label' or 'emoji")
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty("button", "skuId")
            );
            
            EOF();
        }
    }

    [Fact]
    public void ButtonLabelAsChild()
    {
        Graph(
            """
            <button customId="button1">
                This is my label
            </button>
            """
        );
        {
            var button = Node<ButtonComponentNode>(out var buttonNode);

            var label = buttonNode.State!.GetProperty(button.Label);

            Assert.NotNull(label.Value);
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ButtonBuilder(
                    style: global::Discord.ButtonStyle.Primary,
                    label: "This is my label",
                    customId: "button1"
                )
                """
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);
            
            var label = buttonNode.State.GetProperty(button.Label);

            Assert.NotNull(label.Value);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("label", "at most 80 characters in length"),
                label.Value
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);
            
            var customId = buttonNode.State.GetProperty(button.CustomId);

            Assert.NotNull(customId.Value);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("customId", "at most 100 characters in length"),
                customId.Value
            );
            
            EOF();
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
            var button = Node<ButtonComponentNode>(out var buttonNode);
            
            var style = buttonNode.State!.GetProperty(button.Style);

            Assert.NotNull(style.Value);
            
            Validate();

            Renders();
            
            Diagnostic(
                Diagnostics.InvalidEnumVariant("invalid", "Discord.ButtonStyle"),
                style.Value
            );
            
            EOF();
        }
    }
}