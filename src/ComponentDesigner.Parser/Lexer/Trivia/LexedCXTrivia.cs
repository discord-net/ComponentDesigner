using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Parser;

[CollectionBuilder(typeof(LexedCXTrivia), nameof(Create))]
public sealed class LexedCXTrivia(
    ImmutableArray<CXTrivia> trivia
) : IImmutableList<CXTrivia>, IEquatable<LexedCXTrivia>
{
    public bool ContainsWhitespace
        => trivia.Any(x => x is CXTrivia.Token { Kind: CXTriviaTokenKind.Whitespace });

    public bool ContainsNewlines
        => trivia.Any(x => x is CXTrivia.Token { Kind: CXTriviaTokenKind.Newline });

    public bool ContainsComments
        => trivia.Any(x => x is CXTrivia.XmlComment);

    public int CharacterLength { get; } = trivia.Length is 0 ? 0 : trivia.Sum(x => x.Length);

    public static readonly LexedCXTrivia Empty = new([]);

    public static LexedCXTrivia Create(ReadOnlySpan<CXTrivia> trivia)
        => new([..trivia]);
    
    public ImmutableArray<CXTrivia>.Enumerator GetEnumerator() => trivia.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)trivia).GetEnumerator();

    public int Count => trivia.Length;

    public CXTrivia this[int index] => trivia[index];

    public LexedCXTrivia Clear()
        => Empty;

    public int IndexOf(CXTrivia item, int index, int count, IEqualityComparer<CXTrivia>? equalityComparer)
        => trivia.IndexOf(item, index, count, equalityComparer);

    public int LastIndexOf(CXTrivia item, int index, int count, IEqualityComparer<CXTrivia>? equalityComparer)
        => trivia.LastIndexOf(item, index, count, equalityComparer);

    public LexedCXTrivia Add(CXTrivia value)
        => new(trivia.Add(value));

    public LexedCXTrivia AddRange(IEnumerable<CXTrivia> items)
        => new(trivia.AddRange(items));

    public LexedCXTrivia Insert(int index, CXTrivia element)
        => new(trivia.Insert(index, element));

    public LexedCXTrivia InsertRange(int index, IEnumerable<CXTrivia> items)
        => new(trivia.InsertRange(index, items));

    public LexedCXTrivia Remove(CXTrivia value, IEqualityComparer<CXTrivia>? equalityComparer)
        => new(trivia.Remove(value, equalityComparer));

    public LexedCXTrivia RemoveAll(Predicate<CXTrivia> match)
        => new(trivia.RemoveAll(match));

    public LexedCXTrivia RemoveRange(
        IEnumerable<CXTrivia> items,
        IEqualityComparer<CXTrivia>? equalityComparer
    ) => new(trivia.RemoveRange(items));

    public LexedCXTrivia RemoveRange(int index, int count)
        => new(trivia.RemoveRange(index, count));

    public LexedCXTrivia RemoveAt(int index)
        => new(trivia.RemoveAt(index));

    public LexedCXTrivia SetItem(int index, CXTrivia value)
        => new(trivia.SetItem(index, value));

    public LexedCXTrivia Replace(
        CXTrivia oldValue,
        CXTrivia newValue,
        IEqualityComparer<CXTrivia>? equalityComparer
    ) => new(trivia.Replace(oldValue, newValue));


    public bool Equals(LexedCXTrivia? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return
            CharacterLength == other.CharacterLength &&
            this.SequenceEqual(other);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is LexedCXTrivia other && Equals(other);

    public override int GetHashCode()
        => trivia.Aggregate(
            CharacterLength,
            Hash.Combine
        );

    public override string ToString() => string.Join(string.Empty, trivia);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.Clear() => Clear();

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.Add(CXTrivia value) => Add(value);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.AddRange(IEnumerable<CXTrivia> items) => AddRange(items);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.Insert(int index, CXTrivia element) => Insert(index, element);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.InsertRange(int index, IEnumerable<CXTrivia> items)
        => InsertRange(index, items);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.Remove(
        CXTrivia value,
        IEqualityComparer<CXTrivia>? equalityComparer
    ) => Remove(value, equalityComparer);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.RemoveAll(Predicate<CXTrivia> match) => RemoveAll(match);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.RemoveRange(
        IEnumerable<CXTrivia> items,
        IEqualityComparer<CXTrivia>? equalityComparer
    ) => RemoveRange(items, equalityComparer);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.RemoveRange(int index, int count)
        => RemoveRange(index, count);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.RemoveAt(int index)
        => RemoveAt(index);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.SetItem(int index, CXTrivia value)
        => SetItem(index, value);

    IImmutableList<CXTrivia> IImmutableList<CXTrivia>.Replace(
        CXTrivia oldValue,
        CXTrivia newValue,
        IEqualityComparer<CXTrivia>? equalityComparer
    ) => Replace(oldValue, newValue, equalityComparer);

    IEnumerator<CXTrivia> IEnumerable<CXTrivia>.GetEnumerator() => ((IImmutableList<CXTrivia>)trivia).GetEnumerator();
}