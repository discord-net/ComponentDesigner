using System.Diagnostics.CodeAnalysis;

namespace Discord.CX.Parser;

/// <summary>
///     A utility class containing extensions related to the <see cref="CXTokenKind"/> type.
/// </summary>
public static class CXTokenKindExtensions
{
    /// <summary>
    ///     Attempts to get the well-known string representation of a <see cref="CXTokenKind"/>.
    /// </summary>
    /// <param name="kind">The kind to get the text of.</param>
    /// <param name="text">The text representing the <see cref="CXTokenKind"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the given <see cref="CXTokenKind"/> has a well-known string representation;
    ///     otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryGetText(this CXTokenKind kind, [MaybeNullWhen(false)] out string text)
    {
        text = kind switch
        {
            CXTokenKind.LessThan => "<",
            CXTokenKind.GreaterThan => ">",
            CXTokenKind.ForwardSlashGreaterThan => "/>",
            CXTokenKind.LessThanForwardSlash => "</",
            CXTokenKind.Equals => "=",
            CXTokenKind.OpenParenthesis => "(",
            CXTokenKind.CloseParenthesis => ")",
            CXTokenKind.Qualifier => ".",
            CXTokenKind.EOF or CXTokenKind.Invalid => string.Empty,
            _ => null
        };
        
        return text is not null;
    }
}