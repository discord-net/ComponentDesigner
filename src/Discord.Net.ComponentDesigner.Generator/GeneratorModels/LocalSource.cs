using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public readonly record struct LocalSource(
    TextSpan Span,
    string Value
)
{
    public static implicit operator TextSpan(LocalSource self) => self.Span;
    public static implicit operator string(LocalSource self) => self.Value;

    public override string ToString()
        => Value;
}