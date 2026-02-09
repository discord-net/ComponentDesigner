namespace ComponentDesigner.Parser;

/// <summary>
///     A Utility class for text-based operations
/// </summary>
internal static class TextUtils
{
    /// <summary>
    ///     Determines whether the given character is considered a newline.
    /// </summary>
    /// <param name="ch">The character to test.</param>
    /// <returns>
    ///     <see langword="true"/> if the given character is a newline; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsNewline(this char ch)
    {
        return ch is '\r' or '\n' or '\u0085' or '\u2028' or '\u2029';
    }
}
