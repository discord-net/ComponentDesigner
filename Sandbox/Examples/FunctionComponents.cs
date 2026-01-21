using Discord;
using static Discord.ComponentDesigner;

namespace Sandbox.Examples;

public class FunctionComponents
{
    public static CXMessageComponent Consumer()
        => message(
            $"""
            <CompWithChildren name="Foo">
                <text>Foo</text>
            </CompWithChildren>
            
            <CompWithScalarChild foo="b">
                {1}
            </CompWithScalarChild>
            """
        );
    
    public static CXMessageComponent CompWithChildren(string name, [CXChildren] CXMessageComponent component)
        => message(
            $"""
             <container>
                 {component}
             </container>
             """
        );

    public static CXMessageComponent CompWithScalarChild(string foo, [CXChildren] int bar)
    {
        return null!;
    }
}