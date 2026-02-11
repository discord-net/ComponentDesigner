using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

public readonly record struct TextControl(
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia,
    string Value,
    bool ValueContainsNewLines
)
{
    public static readonly TextControl Empty = new(LexedCXTrivia.Empty, LexedCXTrivia.Empty, string.Empty, false);
    
    public bool ContainsNewLines 
        => ValueContainsNewLines || LeadingTrivia.ContainsNewlines || TrailingTrivia.ContainsNewlines;

    public override string ToString()
        => $"{LeadingTrivia}{Value}{TrailingTrivia}";
}