using ComponentDesigner;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;
using Diagnostic = ComponentDesigner.Diagnostic;

namespace UnitTests.GeneratorTests;

public sealed class IncrementalAssuranceTests(ITestOutputHelper output) : BaseGeneratorTest(output)
{
    [Fact]
    public void InterpolatedComponentChanged()
    {
        var a = RunCX(
            """
            <container>
                {foo}
            </container>
            """,
            pretext:
            """
            IMessageComponentBuilder foo = null!;
            """,
            options: new(
                AllowAutoTextDisplays: true
            )
        );

        var b = RunCX(
            """
            <container>
                {foo}
            </container>
            """,
            pretext:
            """
            string foo = null!;
            """,
            options: new(
                AllowAutoTextDisplays: true
            )
        );
        
        AssertRenders(
            a,
            """
            new global::Discord.ContainerBuilder(
                components: 
                [
                    designer.GetValue<global::Discord.IMessageComponentBuilder>(0)
                ]
            )
            """
        );
        
        AssertRenders(
            b,
            """
            new global::Discord.ContainerBuilder(
                components: 
                [
                    new global::Discord.TextDisplayBuilder(
                        content: $"{designer.GetValueAsString(0)}"
                    )
                ]
            )
            """
        );
    }

    [Fact]
    public void GraphDoesntReRender()
    {
        var a = RunCX(
            "<text>Foo</text>"
        );

        var b = RunCX(
            "<text>Foo</text>",
            additionalMethods:
            "public void Foo(){}"
        );

        AssertStepResult(b, TrackingNames.CREATE_GRAPH, IncrementalStepRunReason.Cached);
        AssertStepResult(b, TrackingNames.EMIT_GRAPH, IncrementalStepRunReason.Cached);

        AssertRenders(
            b,
            """
            new global::Discord.TextDisplayBuilder(
                content: "Foo"
            )
            """
        );

        LogRunVisual(b);
    }

    [Fact]
    public void FunctionalComponentDependencyUpdates()
    {
        var a = RunCX(
            "<MyFunc arg=\"foo\" />",
            additionalMethods:
            "public static CXMessageComponent MyFunc(string arg) => CXMessageComponent.Empty;"
        );

        var b = RunCX(
            "<MyFunc arg=\"foo\" />",
            additionalMethods:
            "public static CXMessageComponent MyFunc(int arg) => CXMessageComponent.Empty;"
        );

        // the graph should be cached, while the update graph state should be modified
        AssertStepResult(b, TrackingNames.CREATE_GRAPH, IncrementalStepRunReason.Cached);
        AssertStepResult(b, TrackingNames.UPDATE_GRAPH_EXTERNAL_DEPENDENCIES, IncrementalStepRunReason.Modified);


        var render1 = GetStepValue<EmittedGraph>(a, TrackingNames.RENDER_GRAPH);
        {
            Assert.Equal(
                """
                ..global::TestClass.MyFunc(
                    arg: "foo"
                ).Builders
                """,
                render1.Source
            );
            Assert.Empty(render1.Diagnostics);
        }
        var render2 = GetStepValue<EmittedGraph>(b, TrackingNames.RENDER_GRAPH);
        {
            // second run should error with a type mismatch
            Assert.NotEmpty(render2.Diagnostics);
            Assert.Collection(
                render2.Diagnostics,
                x => AssertDiagnostic(
                    x,
                    Diagnostic.UsingRuntimeValidation("int.Parse")
                )
            );
        }

        LogRunVisual(b);
    }
}