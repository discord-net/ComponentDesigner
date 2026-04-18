using ComponentDesigner;
using ComponentDesigner.Nodes;
using Xunit.Abstractions;

namespace UnitTests.DiscordNet.Components;

public sealed class FunctionalTests(ITestOutputHelper output) : BaseDiscordNetComponentTest(output)
{
    [Fact]
    public void InstancedComponent()
    {
        Graph(
            "<{inst}.MyFunc />",
            hasInterpolations: true,
            pretext: "var inst = new TestInstanceComponent();",
            additionalMembers:
            """
            public class TestInstanceComponent
            {
                public CXMessageComponent MyFunc() => CXMessageComponent.Empty;    
            }
            """
        );
        {
            Component<FunctionalComponentNode>();

            Emits("designer.GetValue<global::TestClass.TestInstanceComponent>(0).MyFunc()");
        }
    }

    [Fact]
    public void BasicParameters()
    {
        Graph(
            "<MyFunc str='abc' integer='123' snowflake='381886978205155338' flag/>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                string str,
                int integer,
                ulong snowflake,
                bool flag
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();

            Emits(
                """
                global::TestClass.MyFunc(
                    str: "abc",
                    integer: 123,
                    snowflake: 381886978205155338,
                    flag: true
                )
                """
            );
        }
    }

    [Fact]
    public void ComponentParameter()
    {
        Graph(
            "<MyFunc foo=(<separator />) />",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                IMessageComponentBuilder foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: new global::Discord.SeparatorBuilder()
                )
                """
            );
        }
    }

    [Fact]
    public void ComponentChild()
    {
        Graph(
            "<MyFunc><separator/></MyFunc>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] IMessageComponentBuilder foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: new global::Discord.SeparatorBuilder()
                )
                """
            );
        }
    }

    [Fact]
    public void TooManyComponentChildren()
    {
        Graph(
            "<MyFunc><separator/><separator/></MyFunc>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] IMessageComponentBuilder foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>(out var node);
            {
                Component<SeparatorComponentNode>();
                Component<SeparatorComponentNode>();
            }

            var fooProperty = node.State.PropertyInfo.Properties.FirstOrDefault(x => x.Name is "foo");

            Assert.NotNull(fooProperty);
            
            Emits(null);
            {
                AssertDiagnostic(Diagnostic.TooManyPropertyValues(fooProperty));
            }
        }
    }

    [Fact]
    public void AllowsManyChildrenComponentsButOnlyOneSpecified()
    {
        Graph(
            "<MyFunc><separator/></MyFunc>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] IEnumerable<IMessageComponentBuilder> foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();
            {
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: 
                    [
                        new global::Discord.SeparatorBuilder()
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void ManyChildComponents()
    {
        Graph(
            "<MyFunc><separator/><separator/><separator/></MyFunc>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] IEnumerable<IMessageComponentBuilder> foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();
            {
                Component<SeparatorComponentNode>();
                Component<SeparatorComponentNode>();
                Component<SeparatorComponentNode>();
            }

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: 
                    [
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.SeparatorBuilder()
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void InterpolatedChildComponent()
    {
        Graph(
            "<MyFunc>{other}</MyFunc>",
            pretext:
            "IMessageComponentBuilder other = null!;",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] IMessageComponentBuilder foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();
            {
                Component<InterpolationComponentNode>();
            }

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: designer.GetValue<global::Discord.IMessageComponentBuilder>(0)
                )
                """
            );
        }
    }

    [Fact]
    public void TextAsScalarChildren()
    {
        Graph(
            "<MyFunc>Hello World!</MyFunc>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] string foo
            ) => null!;
            """
        );
        {
            Component<FunctionalComponentNode>();

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: "Hello World!"
                )
                """
            );
        }
    }

    [Fact]
    public void TextAsComponents()
    {
        Graph(
            "<MyFunc>Hello World!</MyFunc>",
            additionalMembers:
            """
            public static CXMessageComponent MyFunc(
                [CXChildren] IMessageComponentBuilder foo
            ) => null!;
            """,
            options: new(AllowAutoTextDisplays: true)
        );
        {
            Component<FunctionalComponentNode>();
            {
                Component<AutoTextDisplayComponentNode>();
                {
                    Component<TextControlNode>();
                }
            }

            Emits(
                """
                global::TestClass.MyFunc(
                    foo: new global::Discord.TextDisplayBuilder(
                        content: "Hello World!"
                    )
                )
                """
            );
        }
    }
}