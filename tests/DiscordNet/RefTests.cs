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
            <separator ref={ComponentDesigner.CreateRef(out var v)}/>
            """
        );
        {
            Component<SeparatorComponentNode>();

            Emits(
                """
                designer.GetValue<global::Discord.RefBox<global::Discord.IMessageComponentBuilder>>(0).Set(
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
            <separator ref={ComponentDesigner.CreateRef<ButtonBuilder>(out var v)}/>
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
            <separator ref={ComponentDesigner.CreateRef<SeparatorBuilder>(out var v)}/>
            """
        );
        {
            Component<SeparatorComponentNode>();

            Emits(
                """
                designer.GetValue<global::Discord.RefBox<global::Discord.SeparatorBuilder>>(0).Set(
                    new global::Discord.SeparatorBuilder()
                )
                """
            );
        }
    }
}