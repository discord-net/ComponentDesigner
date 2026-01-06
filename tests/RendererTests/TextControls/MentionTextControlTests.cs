using Discord.CX;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class MentionTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void AttributeValues()
    {
        Renders("<mention type='user' id='123' />", "<@123>");
        Renders("<mention type='channel' id='123' />", "<#123>");
        Renders("<mention type='role' id='123' />", "<@&123>");
        Renders("<mention type='cmd' id='123' name='foo' />", "</foo:123>");
    }

    [Fact]
    public void IdChild()
    {
        Renders("<mention type='user'>123</mention>", "<@123>");
        Renders("<mention type='channel'>123</mention>", "<#123>");
        Renders("<mention type='role'>123</mention>", "<@&123>");
        Renders("<mention type='cmd' name='foo'>123</mention>", "</foo:123>");
    }

    [Fact]
    public void NameChild()
    {
        Renders("<mention type='cmd' id='123'>foo</mention>", "</foo:123>");
    }

    [Fact]
    public void TypeInferenceByTag()
    {
        Renders("<user-mention id='123' />", "<@123>");
        Renders("<channel-mention id='123' />", "<#123>");
        Renders("<role-mention id='123' />", "<@&123>");
        Renders("<cmd-mention id='123' name='foo' />", "</foo:123>");
    }

    [Fact]
    public void MissingPropertyDiagnostics()
    {
        Renders("<mention />", null, allowFail: true);
        {
            Diagnostic(Diagnostics.MissingRequiredProperty("mention", "type"));
            Diagnostic(Diagnostics.MissingRequiredProperty("mention", "id"));
        }

        Renders("<user-mention />", null, allowFail: true);
        {
            Diagnostic(Diagnostics.MissingRequiredProperty("user-mention", "id"));
        }

        Renders("<cmd-mention />", null, allowFail: true);
        {
            Diagnostic(Diagnostics.MissingRequiredProperty("cmd-mention", "id"));
            Diagnostic(Diagnostics.MissingRequiredProperty("cmd-mention", "name"));
        }
    }

    [Fact]
    public void InterpolatedChild()
    {
        AssertRendersChild(
            Compilation.GetKnownTypes().IUserType!,
            "<@{designer.GetValue<global::Discord.IUser>(0).Id}>"
        );

        AssertRendersChild(
            Compilation.GetKnownTypes().IChannelType!,
            "<#{designer.GetValue<global::Discord.IChannel>(0).Id}>"
        );

        AssertRendersChild(
            Compilation.GetKnownTypes().IRoleType!,
            "<@&{designer.GetValue<global::Discord.IRole>(0).Id}>"
        );

        AssertRendersChild(
            Compilation.GetKnownTypes().IApplicationCommandType!,
            "</{designer.GetValue<global::Discord.IApplicationCommand>(0).Name}:{designer.GetValue<global::Discord.IApplicationCommand>(0).Id}>"
        );

        void AssertRendersChild(
            INamedTypeSymbol type,
            string expected
        )
        {
            var source = new SourceBuilder()
                .AddSource("<mention>")
                .AddInterpolation("interp")
                .AddSource("</mention>");

            Renders(
                source.StringBuilder.ToString(),
                expected,
                interpolations:
                [
                    new DesignerInterpolationInfo(
                        0,
                        new TextSpan(
                            source.Interpolations[0].Start,
                            source.Interpolations[0].Length
                        ),
                        type,
                        default
                    )
                ]
            );
        }
    }
}