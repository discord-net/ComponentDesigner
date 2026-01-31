using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class FunctionalComponentTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void InstancedComponent()
    {
        Graph(
            """
            <{inst}.MyFunc />
            """,
            hasInterpolations: true,
            pretext:
            """
            var inst = new TestInstanceComponent();
            """,
            additionalMethods:
            """
            public class TestInstanceComponent
            {
                public CXMessageComponent MyFunc() => CXMessageComponent.Empty;    
            }
            """
        );
        {
            Node<FunctionalComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                ..designer.GetValue<global::TestClass.TestInstanceComponent>(0).MyFunc().Builders
                """
            );
        }
    }
    
    [Fact]
    public void InterpolatedChildValue()
    {
        Graph(
            """
            <MyFunc>
                {a}
            </MyFunc>
            """,
            additionalMethods:
            """
            public static CXMessageComponent MyFunc([CXChildren] int children) => null!;
            """,
            pretext:
            """
            int a = 0;
            """
        );
        {
            Node<FunctionalComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                ..global::TestClass.MyFunc(
                    children: designer.GetValue<int>(0)
                ).Builders
                """
            );
        }
    }

    [Fact]
    public void TextAndInterpolatedChildren()
    {
        Graph(
            """
            <MyFunc>
                Text 1 {a} Text 2 {b}
            </MyFunc>
            """,
            additionalMethods:
            """
            public static CXMessageComponent MyFunc([CXChildren] string children) => null!;
            """,
            pretext:
            """
            string a = null!;
            string b = null!;
            """
        );
        {
            Node<FunctionalComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                ..global::TestClass.MyFunc(
                    children: $"Text 1 {designer.GetValueAsString(0)} Text 2 {designer.GetValueAsString(1)}"
                ).Builders
                """
            );
        }
    }

    [Fact]
    public void WithScalarChildren()
    {
        Graph(
            """
            <MyFunc>
                This is some text
            </MyFunc>
            """,
            additionalMethods:
            """
            public static CXMessageComponent MyFunc([CXChildren] string children) => null!;
            """
        );
        {
            Node<FunctionalComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                ..global::TestClass.MyFunc(
                    children: "This is some text"
                ).Builders
                """
            );
        }
    }

    [Fact]
    public void WithComponentChildren()
    {
        Graph(
            """
            <MyFunc>
                <text>Hello</text>
            </MyFunc>
            """,
            additionalMethods:
            """
            public static CXMessageComponent MyFunc([CXChildren] CXMessageComponent children) => null!;
            """
        );
        {
            Node<FunctionalComponentNode>();
            {
                Node<TextDisplayComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                ..global::TestClass.MyFunc(
                    children: new global::Discord.CXMessageComponent([
                        new global::Discord.TextDisplayBuilder(
                            content: "Hello"
                        )
                    ])
                ).Builders
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ChildOfContainer()
    {
        Graph(
            """
            <container>
                <MyFunc />
            </container>
            """,
            additionalMethods:
            """
            public static MessageComponent MyFunc() => null!;
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<FunctionalComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        ..global::TestClass.MyFunc().Components.Select(x => x.ToBuilder())
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void CommonScalarParameterTypes()
    {
        Graph(
            """
            <MyFunc 
                p1="str"
                p2="123"
                p3="blue"
            />
            """,
            additionalMethods:
            """
            public static MessageComponent MyFunc(
                string p1,
                int p2,
                Discord.Color p3
            ) => null!;
            """
        );
        {
            Node<FunctionalComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                ..global::TestClass.MyFunc(
                    p1: "str",
                    p2: 123,
                    p3: global::Discord.Color.Blue
                ).Components.Select(x => x.ToBuilder())
                """
            );

            EOF();
        }
    }

    [Fact]
    public void AllValidComponentTypesAsReturnType()
    {
        TestReturnType(
            "MessageComponent",
            "..global::TestClass.MyFunc().Components.Select(x => x.ToBuilder())"
        );

        TestReturnType(
            "CXMessageComponent",
            "..global::TestClass.MyFunc().Builders"
        );

        TestReturnType(
            "IMessageComponentBuilder",
            "global::TestClass.MyFunc()"
        );

        TestReturnType(
            "IMessageComponent",
            "global::TestClass.MyFunc().ToBuilder()"
        );

        TestReturnType(
            "IEnumerable<IMessageComponent>",
            "..global::TestClass.MyFunc().Select(x => x.ToBuilder())"
        );

        // ..global::TestClass.MyFunc().Select(x => x)
        TestReturnType(
            "IEnumerable<IMessageComponentBuilder>",
            "..global::TestClass.MyFunc()"
        );

        TestReturnType(
            "IEnumerable<MessageComponent>",
            "..global::TestClass.MyFunc().SelectMany(x => x.Components.Select(x => x.ToBuilder()))"
        );

        TestReturnType(
            "IEnumerable<CXMessageComponent>",
            "..global::TestClass.MyFunc().SelectMany(x => x.Builders)"
        );


        void TestReturnType(string type, string expectedRenderedValue)
        {
            Graph(
                """
                <container>
                    <MyFunc />
                </container>
                """,
                additionalMethods:
                $"""
                 public static {type} MyFunc() => null!;
                 """
            );
            {
                Node<ContainerComponentNode>();
                {
                    Node<FunctionalComponentNode>();
                }

                Validate(hasErrors: false);

                Renders(
                    $$"""
                      new global::Discord.ContainerBuilder()
                      {
                          Components =
                          [
                              {{expectedRenderedValue}}
                          ]
                      }
                      """
                );

                EOF();
            }
        }
    }
}