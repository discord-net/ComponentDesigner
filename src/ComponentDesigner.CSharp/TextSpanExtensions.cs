using Microsoft.CodeAnalysis.Text;

namespace ComponentDesigner.CSharp;

public static class TextSpanExtensions
{
    extension(TextSpan textSpan)
    {
        public CXTextSpan AsCXTextSpan => new(textSpan.Start, textSpan.Length);
    }

    extension(CXTextSpan textSpan)
    {
        public TextSpan AsRoslynTextSpan => new(textSpan.Start, textSpan.Length);
    }
}