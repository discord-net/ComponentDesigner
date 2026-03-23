using ComponentDesigner.Parser;

namespace ComponentDesigner.Parser;

public interface IContainsTrivia
{
    /// <summary>
    ///     Gets the lexed leading trivia belonging to this object.
    /// </summary>
    LexedCXTrivia LeadingTrivia { get; }
    
    /// <summary>
    ///     Gets the lexed trailing belonging to this object.
    /// </summary>
    LexedCXTrivia TrailingTrivia { get; }
}

public readonly record struct ContainsTrivia<T>(
    T Value,
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia
) : IContainsTrivia
{
    public static implicit operator T(ContainsTrivia<T> self) => self.Value;
}

public static class ContainsTriviaExtensions
{
    extension<T>(T value)
    {
        public ContainsTrivia<T> WithNoTrivia
            => new ContainsTrivia<T>(value, LexedCXTrivia.Empty, LexedCXTrivia.Empty);
        
        public ContainsTrivia<T> WithTriviaFrom<U>(U trivia) where U : IContainsTrivia
            => new ContainsTrivia<T>(value, trivia.LeadingTrivia, trivia.TrailingTrivia);
        
        public ContainsTrivia<T> WithTrivia(LexedCXTrivia leadingTrivia, LexedCXTrivia trailingTrivia)
            => new ContainsTrivia<T>(value, leadingTrivia, trailingTrivia);
    }
}