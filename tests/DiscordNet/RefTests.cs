using ComponentDesigner;
using ComponentDesigner.Nodes;
using UnitTests.DiscordNet.Components;
using UnitTests.GeneratorTests;
using UnitTests.Graph.Components;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet;

public class RefTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void BasicRef()
    {
        Graph(
            """
            <separator ref={cx.CreateRef(out var v)}/>
            """
        );
        {
            Component<SeparatorComponentNode>();

            Emits(
                """
                syntax.GetValue<global::ComponentDesigner.RefBox<global::Discord.IMessageComponentBuilder>>(0).Set(
                    new global::Discord.SeparatorBuilder()
                )
                """
            );
        }
    }

    [Fact]
    public void RefWithBadType()
    {
        Graph(
            """
            <separator ref={cx.CreateRef<ButtonBuilder>(out var v)}/>
            """
        );
        {
            Component<SeparatorComponentNode>();

            Emits(null);
            {
                AssertDiagnostic(Diagnostic.TypeMismatch("Discord.SeparatorBuilder", "Discord.ButtonBuilder"));
            }
        }
    }
    
    [Fact]
    public void RefWithNarrowType()
    {
        Graph(
            """
            <separator ref={cx.CreateRef<SeparatorBuilder>(out var v)}/>
            """
        );
        {
            Component<SeparatorComponentNode>();

            Emits(
                """
                syntax.GetValue<global::ComponentDesigner.RefBox<global::Discord.SeparatorBuilder>>(0).Set(
                    new global::Discord.SeparatorBuilder()
                )
                """
            );
        }
    }
}