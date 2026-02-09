using System;
using System.Linq;

namespace ComponentDesigner.Parser;

public static class TriviaExtensions
{
    extension(LexedCXTrivia trivia)
    {
        public LexedCXTrivia NewlinesOnly()
            => [..trivia.Where(x => x is CXTrivia.Token { Kind: CXTriviaTokenKind.Newline })];

        public LexedCXTrivia WhitespaceOnly()
            => [..trivia.Where(x => x.IsWhitespaceTrivia)];

        public LexedCXTrivia ToIndentationOnly()
        {
            var ws = trivia.Where(x => x.IsWhitespaceTrivia).ToArray().AsSpan();

            // find the last newline
            var fromIndex = ws.Length - 1;
            for (; fromIndex >= 0; fromIndex--)
            {
                if (ws[fromIndex] is CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) break;
            }

            if (fromIndex is -1) return [..ws];

            return [..ws.Slice(fromIndex + 1)];
        }

        public LexedCXTrivia TrimLeadingSyntaxIndentation()
        {
            for (var i = 0; i < trivia.Count; i++)
            {
                var item = trivia[i];

                if (item is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                return [..trivia.Skip(i + 1)];
            }

            return trivia;
        }

        public LexedCXTrivia TrimTrailingSyntaxIndentation()
        {
            for (var i = trivia.Count - 1; i >= 0; i--)
            {
                var item = trivia[i];

                if (item is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                return [..trivia.Take(i)];
            }

            return trivia;
        }
    }
}