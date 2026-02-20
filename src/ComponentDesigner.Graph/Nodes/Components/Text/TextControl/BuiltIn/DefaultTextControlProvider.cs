using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

public class DefaultTextControlProvider : ITextControlProvider
{
    public static readonly DefaultTextControlProvider Instance = new();
    
    private static readonly Dictionary<string, TextControlFactory> _textControls;

    static DefaultTextControlProvider()
    {
        _textControls = new();

        AddFactory<TextControlElement.Bold>("b", "bold", "strong");
        AddFactory<TextControlElement.Italic>("i", "italic", "italics", "strong");
        AddFactory<TextControlElement.Underline>("u", "mark", "underline", "ins");
        AddFactory<TextControlElement.StrikeThrough>("del", "strike", "strike-through", "strikethrough");
        AddFactory<TextControlElement.SubText>("sub", "subtext", "sub-text", "small");
        AddFactory<TextControlElement.Link>("a", "link");

        AddFactory(
            (element, children) => new TextControlElement.List(
                element, TextControlElement.ListKind.Unordered, children
            ),
            "ul", "list"
        );

        AddFactory(
            (element, children) => new TextControlElement.List(
                element, TextControlElement.ListKind.Ordered, children
            ),
            "ol",
            "ordered-list",
            "orderedlist"
        );

        AddFactory<TextControlElement.ListItem>("li");
        AddFactory<TextControlElement.Code>("c", "code", "codeblock", "block");
        AddFactory<TextControlElement.Quote>("q", "quote", "blockquote", "block-quote");
        AddFactory<TextControlElement.Spoiler>("hidden", "spoiler", "obfuscated");
        AddFactory<TextControlElement.LineBreak>("br", "break", "line-break");

        AddFactory(
            (element, children) => new TextControlElement.Heading(
                element, TextControlElement.HeadingVariant.H1, children
            ),
            "h1"
        );
        
        AddFactory(
            (element, children) => new TextControlElement.Heading(
                element, TextControlElement.HeadingVariant.H1, children
            ),
            "h2"
        );
        
        AddFactory(
            (element, children) => new TextControlElement.Heading(
                element, TextControlElement.HeadingVariant.H1, children
            ),
            "h3"
        );
    }

    private static void AddFactory<T>(params string[] names) where T : TextControlElement
    {
        var factory = AsFactory<T>();

        AddFactory(factory, names);
    }

    private static void AddFactory(TextControlFactory factory, params string[] names)
    {
        foreach (var name in names)
        {
            _textControls[name] = factory;
        }
    }

    private static TextControlFactory AsFactory<T>() where T : TextControlElement
    {
        var constructor = typeof(T).GetConstructor([typeof(CXElement), typeof(IReadOnlyList<TextControlElement>)]);

        if (constructor is null) throw new InvalidOperationException();

        return (element, children) => (T)constructor.Invoke([element, children]);
    }

    public bool TryGetTextControlFactory(
        CXElement element,
        [MaybeNullWhen(false)] out TextControlFactory factory
    ) => _textControls.TryGetValue(element.Identifier, out factory);

    public bool IsValidChild(
        TextControlElement parent,
        TextControlElement child
    )
    {
        // TODO
        return true;
    }
}