using System.Collections;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class NodeList : 
    IList<GraphNode>,
    IReadOnlyList<GraphNode>,
    IEquatable<NodeList>
{
    public int Count => _ids?.Count ?? 0;
    
    public bool IsReadOnly => false;
    
    public GraphNode this[int index]
    {
        get
        {
            if (_ids is null || _ids.Count <= index) throw new IndexOutOfRangeException();

            return _tree[_ids[index]];
        }
        set
        {
            if (!_tree.Equals(value.Tree))
                throw new InvalidOperationException();

            if (_ids is null || index >= _ids.Count)
                throw new IndexOutOfRangeException();

            _ids[index] = value.Id;
        }
    }

    private List<int>? _ids;
    private readonly CXComponentTree _tree;
    
    internal NodeList(CXComponentTree tree)
    {
        _tree = tree;
    }

    private NodeList(CXComponentTree tree, List<int>? ids)
    {
        _tree = tree;
        _ids = ids;
    }

    public NodeList WithTree(CXComponentTree tree)
        => new(tree, _ids);

    public bool Equals(NodeList? other)
        => other is not null &&
           Count == other.Count &&
           (Count is 0 || _ids!.SequenceEqual(other._ids!));

    public override bool Equals(object? obj)
        => obj is NodeList other && Equals(other);

    public override int GetHashCode()
        => Count is 0 ? 0 : _ids!.Aggregate(0, Hash.Combine);

    public void Add(GraphNode graphNode)
    {
        if (!_tree.Equals(graphNode.Tree)) throw new InvalidOperationException("Trees differ");

        _ids ??= [];
        _ids.Add(graphNode.Id);
    }

    public void Clear() => _ids?.Clear();

    public bool Contains(GraphNode item)
        => _ids is not null && _tree.Equals(item.Tree) && _ids.Contains(item.Id);

    public void CopyTo(GraphNode[] array, int arrayIndex)
    {
        if (_ids is null) return;

        for (var i = 0; i < _ids.Count; i++)
        {
            array[i + arrayIndex] = _tree[_ids[i]];
        }
    }

    public bool Remove(GraphNode item)
        => _ids is not null && _tree.Equals(item.Tree) && _ids.Remove(item.Id);

    public int IndexOf(GraphNode item)
    {
        if (_ids is null || !_tree.Equals(item.Tree)) return -1;

        return _ids.IndexOf(item.Id);
    }

    public void Insert(int index, GraphNode item)
    {
        if (_ids is null || !_tree.Equals(item.Tree))
            throw new InvalidOperationException();
        
        _ids.Insert(index, item.Id);
    }

    public void RemoveAt(int index)
        => _ids?.RemoveAt(index);
    
    public IEnumerator<GraphNode> GetEnumerator()
    {
        if(_ids is null or {Count: 0}) yield break;

        for (var i = 0; i < _ids.Count; i++)
            yield return _tree[_ids[i]];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}