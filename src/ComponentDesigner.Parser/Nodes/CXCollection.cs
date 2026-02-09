using System.Collections;
using System.Collections.Generic;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a non-specific collection of <see cref="ICXNode"/>s.
/// </summary>
public interface ICXCollection : ICXNode
{
    /// <summary>
    ///     Gets the number of <see cref="ICXNode"/>s contained in this collection.
    /// </summary>
    int Count { get; }
    
    /// <summary>
    ///     Gets a single <see cref="ICXNode"/> given an index.
    /// </summary>
    /// <param name="index">The index of the <see cref="ICXNode"/> to get.</param>
    ICXNode this[int index] { get; }

    /// <summary>
    ///     Converts this <see cref="ICXCollection"/> into a read-only list of <see cref="ICXNode"/>s.
    /// </summary>
    /// <returns>A read-only list of <see cref="ICXNode"/>s.</returns>
    IReadOnlyList<ICXNode> ToList();
}

/// <summary>
///     Represents a generic collection of <see cref="ICXNode"/>s.
/// </summary>
/// <typeparam name="T">The inner type of the collection.</typeparam>
public sealed class CXCollection<T> :
    CXNode,
    ICXCollection,
    IReadOnlyList<T>
    where T : class, ICXNode
{
    /// <inheritdoc/>
    public T this[int index] => _items[index];
    
    /// <inheritdoc/>
    public int Count => _items.Count;

    private readonly IReadOnlyList<T> _items;

    /// <summary>
    ///     Constructs a new <see cref="CXCollection{T}"/> with the provided collection of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="items"></param>
    public CXCollection(params IEnumerable<T> items)
    {
        Slot((_items = [..items]));
    }


    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _items).GetEnumerator();
    
    /// <inheritdoc/>
    ICXNode ICXCollection.this[int index] => this[index];
    
    /// <inheritdoc/>
    int ICXCollection.Count => Count;
    
    /// <inheritdoc/>
    IReadOnlyList<ICXNode> ICXCollection.ToList() => _items;
}
