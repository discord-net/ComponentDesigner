namespace ComponentDesigner;

public static class ListExtensions
{
    public static void AddRange<T>(this IList<T> list, params IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            list.Add(item);
        }
    }
}