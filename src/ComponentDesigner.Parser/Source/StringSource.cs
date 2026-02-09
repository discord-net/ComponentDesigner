namespace ComponentDesigner.Parser;

partial class CXSourceText
{
    /// <summary>
    ///     Constructs a new <see cref="CXSourceText"/> representing the given <see langword="string"/>.
    /// </summary>
    /// <param name="source">The <see langword="string"/> source to represent.</param>
    /// <returns>
    ///     A <see cref="CXSourceText"/> representing the provided <paramref name="source"/>.
    /// </returns>
    public static CXSourceText From(string source) => new StringSource(source);
    
    /// <summary>
    ///     Represents a <see cref="CXSourceText"/> wrapping a <see langword="string"/>.
    /// </summary>
    /// <param name="text">The underlying wrapped <see langword="string"/>.</param>
    internal sealed class StringSource(string text) : CXSourceText
    {
        /// <summary>
        ///     Gets the underlying wrapped <see langword="string"/>.
        /// </summary>
        public string Text { get; } = text;

        /// <inheritdoc/>
        public override char this[int i] => Text[i];
        
        /// <inheritdoc/>
        public override int Length => Text.Length;

        /// <inheritdoc/>
        public override string this[int start, int length]
            => Text.Substring(start, length);

        /// <inheritdoc/>
        public override string ToString() => Text;
    }
}
