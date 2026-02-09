using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ComponentDesigner.Parser.Util;

// based off of https://github.com/dotnet/roslyn/blob/4e1e9a3c7ef8c65012f29674a1e170c0ba015159/src/Dependencies/Collections/Segmented/SegmentedArray%601.cs#L22

/// <summary>
///     Defines a fixed-size collection with the same API surface and behavior as an "SZArray", which is a
///     single-dimensional zero-based array commonly represented in C# as <c>T[]</c>. The implementation of this
///     collection uses segmented arrays to avoid placing objects on the Large Object Heap.
/// </summary>
/// <typeparam name="T">The type of elements stored in the array.</typeparam>
internal readonly struct SegmentedArray<T> :
    ICloneable,
    IList,
    IStructuralComparable,
    IStructuralEquatable,
    IList<T>,
    IReadOnlyList<T>,
    IEquatable<SegmentedArray<T>>
{
    /// <summary>
    /// The number of elements in each page of the segmented array of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// <para>The segment size is calculated according to <see cref="Unsafe.SizeOf{T}"/>, performs the IL operation
    /// defined by <see cref="OpCodes.Sizeof"/>. ECMA-335 defines this operation with the following note:</para>
    ///
    /// <para><c>sizeof</c> returns the total size that would be occupied by each element in an array of this type –
    /// including any padding the implementation chooses to add. Specifically, array elements lie <c>sizeof</c>
    /// bytes apart.</para>
    /// </remarks>
    private static int SegmentSize => SegmentedArrayHelper.GetSegmentSize<T>();
    
    /// <summary>
    /// The bit shift to apply to an array index to get the page index within <see cref="_items"/>.
    /// </summary>
    private static int SegmentShift => SegmentedArrayHelper.GetSegmentShift<T>();
    
    /// <summary>
    /// The bit mask to apply to an array index to get the index within a page of <see cref="_items"/>.
    /// </summary>
    private static int OffsetMask => SegmentedArrayHelper.GetOffsetMask<T>();

    public bool IsFixedSize => true;

    public bool IsReadOnly => true;

    public bool IsSynchronized => false;

    public int Length => _length;

    public object SyncRoot => _items;

    public ref T this[int index]
        => ref _items[index >> SegmentShift][index & OffsetMask];

    private readonly int _length;
    private readonly T[][] _items;

    public SegmentedArray(int length)
    {
        if (length is 0)
        {
            _items = [];
            _length = 0;
        }
        else
        {
            _items = new T[(length + SegmentSize - 1) >> SegmentShift][];

            for (var i = 0; i < _items.Length - 1; i++)
                _items[i] = new T[SegmentSize];

            var lastPageSize = length - ((_items.Length - 1) << SegmentShift);

            _items[_items.Length - 1] = new T[lastPageSize];
            _length = length;
        }
    }

    private SegmentedArray(int length, T[][] items)
    {
        _length = length;
        _items = items;
    }

    public object Clone()
    {
        var items = (T[][])_items.Clone();
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = (T[])items[i].Clone();
        }

        return new SegmentedArray<T>(Length, items);
    }

    public void CopyTo(Array array, int index)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            _items[i].CopyTo(array, index + (i * SegmentSize));
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is SegmentedArray<T> other
               && Equals(other);
    }

    public override int GetHashCode()
    {
        return _items.GetHashCode();
    }

    public bool Equals(SegmentedArray<T> other)
    {
        return _items == other._items;
    }

    public Enumerator GetEnumerator()
        => new(this);

    int ICollection.Count => Length;

    int ICollection<T>.Count => Length;

    int IReadOnlyCollection<T>.Count => Length;

    T IReadOnlyList<T>.this[int index] => this[index];

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    object? IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value!;
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            ICollection<T> collection = _items[i];
            collection.CopyTo(array, arrayIndex + (i * SegmentSize));
        }
    }

    int IList.Add(object? value) => throw new NotSupportedException();

    void ICollection<T>.Add(T value) => throw new NotSupportedException();

    void IList.Clear()
    {
        foreach (IList list in _items)
        {
            list.Clear();
        }
    }

    void ICollection<T>.Clear() => throw new NotSupportedException();

    bool IList.Contains(object? value)
    {
        foreach (IList list in _items)
        {
            if (list.Contains(value))
                return true;
        }

        return false;
    }

    bool ICollection<T>.Contains(T value)
    {
        foreach (ICollection<T> collection in _items)
        {
            if (collection.Contains(value))
                return true;
        }

        return false;
    }

    int IList.IndexOf(object? value)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            IList list = _items[i];
            var index = list.IndexOf(value);
            if (index >= 0)
            {
                return index + i * SegmentSize;
            }
        }

        return -1;
    }

    int IList<T>.IndexOf(T value)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            IList<T> list = _items[i];
            var index = list.IndexOf(value);
            if (index >= 0)
            {
                return index + i * SegmentSize;
            }
        }

        return -1;
    }

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    void IList<T>.Insert(int index, T value) => throw new NotSupportedException();

    void IList.Remove(object? value) => throw new NotSupportedException();

    bool ICollection<T>.Remove(T value) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    int IStructuralComparable.CompareTo(object? other, IComparer comparer)
    {
        if (other is null)
            return 1;

        if (other is not SegmentedArray<T> o
            || Length != o.Length)
        {
            throw new ArgumentException("Others length is not equal", nameof(other));
        }

        for (var i = 0; i < Length; i++)
        {
            var result = comparer.Compare(this[i], o[i]);
            if (result != 0)
                return result;
        }

        return 0;
    }

    bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
    {
        if (other is null)
            return false;

        if (other is not SegmentedArray<T> o)
            return false;

        if (ReferenceEquals(_items, o._items))
            return true;

        if (Length != o.Length)
            return false;

        for (var i = 0; i < Length; i++)
        {
            if (!comparer.Equals(this[i], o[i]))
                return false;
        }

        return true;
    }

    int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
    {
        _ = comparer ?? throw new ArgumentNullException(nameof(comparer));

        var ret = 0;
        for (var i = Length >= 8 ? Length - 8 : 0; i < Length; i++)
        {
#if NET
                ret = HashCode.Combine(comparer.GetHashCode(this[i]!), ret);
#else
            ret = unchecked((ret * (int)0xA5555529) + comparer.GetHashCode(this[i]!));
#endif
        }

        return ret;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly T[][] _items;
        private int _nextItemSegment;
        private int _nextItemIndex;
        private T _current;

        public Enumerator(SegmentedArray<T> array)
        {
            _items = array._items;
            _nextItemSegment = 0;
            _nextItemIndex = 0;
            _current = default!;
        }

        public readonly T Current => _current;
        readonly object? IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_items.Length == 0)
                return false;

            if (_nextItemIndex == _items[_nextItemSegment].Length)
            {
                if (_nextItemSegment == _items.Length - 1)
                {
                    return false;
                }

                _nextItemSegment++;
                _nextItemIndex = 0;
            }

            _current = _items[_nextItemSegment][_nextItemIndex];
            _nextItemIndex++;
            return true;
        }

        public void Reset()
        {
            _nextItemSegment = 0;
            _nextItemIndex = 0;
            _current = default!;
        }
    }
}

internal static class SegmentedArrayHelper
{
    // This is the threshold where Introspective sort switches to Insertion sort.
    // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
    // Large value types may benefit from a smaller number.
    internal const int IntrosortSizeThreshold = 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetSegmentSize<T>()
    {
        return Unsafe.SizeOf<T>() switch
        {
            1 => 65536,
            2 => 32768,
            4 => 16384,
            8 => 8192,
            12 => 4096,
            16 => 4096,
            24 => 2048,
            28 => 2048,
            32 => 2048,
            40 => 2048,
            64 => 1024,
#if NETCOREAPP3_0_OR_GREATER
                _ => InlineCalculateSegmentSize(Unsafe.SizeOf<T>()),
#else
            _ => FallbackSegmentHelper<T>.SegmentSize,
#endif
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetSegmentShift<T>()
    {
        return Unsafe.SizeOf<T>() switch
        {
            1 => 16,
            2 => 15,
            4 => 14,
            8 => 13,
            12 => 12,
            16 => 12,
            24 => 11,
            28 => 11,
            32 => 11,
            40 => 11,
            64 => 10,
#if NETCOREAPP3_0_OR_GREATER
                _ => InlineCalculateSegmentShift(Unsafe.SizeOf<T>()),
#else
            _ => FallbackSegmentHelper<T>.SegmentShift,
#endif
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetOffsetMask<T>()
    {
        return Unsafe.SizeOf<T>() switch
        {
            1 => 65535,
            2 => 32767,
            4 => 16383,
            8 => 8191,
            12 => 4095,
            16 => 4095,
            24 => 2047,
            28 => 2047,
            32 => 2047,
            40 => 2047,
            64 => 1023,
#if NETCOREAPP3_0_OR_GREATER
                _ => InlineCalculateOffsetMask(Unsafe.SizeOf<T>()),
#else
            _ => FallbackSegmentHelper<T>.OffsetMask,
#endif
        };
    }

    /// <summary>
    /// Calculates the maximum number of elements of size <paramref name="elementSize"/> which can fit into an array
    /// which has the following characteristics:
    /// <list type="bullet">
    /// <item><description>The array can be allocated in the small object heap.</description></item>
    /// <item><description>The array length is a power of 2.</description></item>
    /// </list>
    /// </summary>
    /// <param name="elementSize">The size of the elements in the array.</param>
    /// <returns>The segment size to use for small object heap segmented arrays.</returns>
    private static int CalculateSegmentSize(int elementSize)
    {
        // Default Large Object Heap size threshold
        const int Threshold = 85000;

        var segmentSize = 2;
        while (ArraySize(elementSize, segmentSize << 1) < Threshold)
        {
            segmentSize <<= 1;
        }

        return segmentSize;

        static int ArraySize(int elementSize, int segmentSize)
        {
            // Array object header, plus space for the elements
            return (2 * IntPtr.Size + 8) + (elementSize * segmentSize);
        }
    }

    /// <summary>
    /// Calculates a shift which can be applied to an absolute index to get the page index within a segmented array.
    /// </summary>
    /// <param name="segmentSize">The number of elements in each page of the segmented array. Must be a power of 2.</param>
    /// <returns>The shift to apply to the absolute index to get the page index within a segmented array.</returns>
    private static int CalculateSegmentShift(int segmentSize)
    {
        var segmentShift = 0;
        while (0 != (segmentSize >>= 1))
        {
            segmentShift++;
        }

        return segmentShift;
    }

    /// <summary>
    /// Calculates a mask, which can be applied to an absolute index to get the index within a page of a segmented
    /// array.
    /// </summary>
    /// <param name="segmentSize">The number of elements in each page of the segmented array. Must be a power of 2.</param>
    /// <returns>The bit mask to obtain the index within a page from an absolute index within a segmented array.</returns>
    private static int CalculateOffsetMask(int segmentSize)
    {
        Debug.Assert(segmentSize == 1 || (segmentSize & (segmentSize - 1)) == 0, "Expected size of 1, or a power of 2");
        return segmentSize - 1;
    }

    // Faster inline implementation for NETCOREAPP to avoid static constructors and non-inlineable
    // generics with runtime lookups
#if NETCOREAPP3_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InlineCalculateSegmentSize(int elementSize)
        {
            return 1 << InlineCalculateSegmentShift(elementSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InlineCalculateSegmentShift(int elementSize)
        {
            const uint Threshold = 85000;
            return System.Numerics.BitOperations.Log2((uint)((Threshold / elementSize) - (2 * Unsafe.SizeOf<object>() + 8)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int InlineCalculateOffsetMask(int elementSize)
        {
            return InlineCalculateSegmentSize(elementSize) - 1;
        }
#endif

#if !NETCOREAPP3_0_OR_GREATER
    private static class FallbackSegmentHelper<T>
    {
        public static readonly int SegmentSize = CalculateSegmentSize(Unsafe.SizeOf<T>());
        public static readonly int SegmentShift = CalculateSegmentShift(SegmentSize);
        public static readonly int OffsetMask = CalculateOffsetMask(SegmentSize);
    }
#endif
}