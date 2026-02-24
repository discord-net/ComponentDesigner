namespace ComponentDesigner;

public static class TextSpanExtensions
{
    extension(CXTextSpan)
    {
        public static CXTextSpan From<T>(
            IList<T> collection,
            int? start = null,
            int? count = null
        ) where T : ISourceLocatable
        {
            if (collection.Count is 0) return default;

            var startIndex = Math.Min(
                start ?? 0,
                collection.Count - 1
            );

            var endIndex = count is not null
                ? Math.Min(collection.Count - 1, count.Value)
                : collection.Count - 1;

            return CXTextSpan.FromBounds(
                collection[startIndex].TextSpan.Start,
                collection[endIndex].TextSpan.End
            );
        }
        
        public static CXTextSpan From<T>(
            IReadOnlyList<T> collection,
            int? start = null,
            int? count = null
        ) where T : ISourceLocatable
        {
            if (collection.Count is 0) return default;

            var startIndex = Math.Min(
                start ?? 0,
                collection.Count - 1
            );

            var endIndex = count is not null
                ? Math.Min(collection.Count - 1, count.Value)
                : collection.Count - 1;

            return CXTextSpan.FromBounds(
                collection[startIndex].TextSpan.Start,
                collection[endIndex].TextSpan.End
            );
        }
    }
}