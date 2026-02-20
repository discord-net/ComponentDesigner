using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public delegate TextControlElement TextControlFactory(
    CXElement element, IReadOnlyList<TextControlElement> children
);

public interface ITextControlProvider
{
    bool TryGetTextControlFactory(
        CXElement element,
        [MaybeNullWhen(false)] out TextControlFactory factory
    );

    bool IsValidChild(TextControlElement parent, TextControlElement child);
}