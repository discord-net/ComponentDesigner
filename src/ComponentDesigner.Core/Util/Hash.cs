using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ComponentDesigner.Util;

/// <summary>
///     A utility class to compute basic hash codes.
/// </summary>
public static class Hash
{
    private const int CombinePrime = 397;

    /// <summary>
    ///     Gets the has of a single value.
    /// </summary>
    /// <param name="value">The value to get the hash of.</param>
    /// <typeparam name="T">The type of the value to hash.</typeparam>
    /// <returns>
    ///     The hash code of the provided value, <c>0</c> if the value is null.
    /// </returns>
    public static int Combine<T>(T value) => value?.GetHashCode() ?? 0;

    /// <summary>
    ///     Combines the hashcodes of two values together.
    /// </summary>
    /// <param name="a">The first value to hash.</param>
    /// <param name="b">The second value to hash.</param>
    /// <typeparam name="T">The type of the first value to hash.</typeparam>
    /// <typeparam name="U">The type of the second value to hash.</typeparam>
    /// <returns>The combined hashcode of the two values.</returns>
    public static int Combine<T, U>(T a, U b)
        => unchecked(((a?.GetHashCode() ?? 0) * CombinePrime) ^ ((b?.GetHashCode() ?? 0) * CombinePrime));

    /// <summary>
    ///     Combines the hashcodes of the values within a <see cref="ReadOnlySpan{T}"/> 
    /// </summary>
    /// <param name="args">The values to hash.</param>
    /// <returns>
    ///     The combined hashcode of the values within the provided span.
    /// </returns>
    public static int Combine(params ReadOnlySpan<object?> args)
    {
        var result = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var item = args[i];
            result ^= ((item?.GetHashCode() ?? 0) * CombinePrime);
        }

        return result;
    }

    /// <summary>
    ///     Combines the hashcodes of the values within a <see cref="IEnumerable{T}"/> 
    /// </summary>
    /// <param name="args">The values to hash.</param>
    /// <returns>
    ///     The combined hashcode of the values within the provided collection.
    /// </returns>
    public static int Combine(params IEnumerable<object?>? args)
    {
        if (args is null) return 0;

        return args.Aggregate(0, Combine);
    }

    /// <summary>
    /// The offset bias value used in the FNV-1a algorithm
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    internal const int FnvOffsetBias = unchecked((int)2166136261);

    /// <summary>
    /// The generative factor used in the FNV-1a algorithm
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    internal const int FnvPrime = 16777619;

    /// <summary>
    /// Compute the FNV-1a hash of a sequence of bytes
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="data">The sequence of bytes</param>
    /// <returns>The FNV-1a hash of <paramref name="data"/></returns>
    internal static int GetFNVHashCode(byte[] data)
    {
        int hashCode = Hash.FnvOffsetBias;

        for (int i = 0; i < data.Length; i++)
        {
            hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
        }

        return hashCode;
    }

    /// <summary>
    /// Compute the FNV-1a hash of a sequence of bytes and determines if the byte
    /// sequence is valid ASCII and hence the hash code matches a char sequence
    /// encoding the same text.
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="data">The sequence of bytes that are likely to be ASCII text.</param>
    /// <param name="isAscii">True if the sequence contains only characters in the ASCII range.</param>
    /// <returns>The FNV-1a hash of <paramref name="data"/></returns>
    internal static int GetFNVHashCode(ReadOnlySpan<byte> data, out bool isAscii)
    {
        int hashCode = Hash.FnvOffsetBias;

        byte asciiMask = 0;

        for (int i = 0; i < data.Length; i++)
        {
            byte b = data[i];
            asciiMask |= b;
            hashCode = unchecked((hashCode ^ b) * Hash.FnvPrime);
        }

        isAscii = (asciiMask & 0x80) == 0;
        return hashCode;
    }

    /// <summary>
    /// Compute the FNV-1a hash of a sequence of bytes
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="data">The sequence of bytes</param>
    /// <returns>The FNV-1a hash of <paramref name="data"/></returns>
    internal static int GetFNVHashCode(ImmutableArray<byte> data)
    {
        int hashCode = Hash.FnvOffsetBias;

        for (int i = 0; i < data.Length; i++)
        {
            hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
        }

        return hashCode;
    }

    /// <summary>
    /// Compute the hashcode of a sub-string using FNV-1a
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// Note: FNV-1a was developed and tuned for 8-bit sequences. We're using it here
    /// for 16-bit Unicode chars on the understanding that the majority of chars will
    /// fit into 8-bits and, therefore, the algorithm will retain its desirable traits
    /// for generating hash codes.
    /// </summary>
    internal static int GetFNVHashCode(ReadOnlySpan<char> data)
    {
        return CombineFNVHash(Hash.FnvOffsetBias, data);
    }

    /// <summary>
    /// Compute the hashcode of a sub-string using FNV-1a
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// Note: FNV-1a was developed and tuned for 8-bit sequences. We're using it here
    /// for 16-bit Unicode chars on the understanding that the majority of chars will
    /// fit into 8-bits and, therefore, the algorithm will retain its desirable traits
    /// for generating hash codes.
    /// </summary>
    /// <param name="text">The input string</param>
    /// <param name="start">The start index of the first character to hash</param>
    /// <param name="length">The number of characters, beginning with <paramref name="start"/> to hash</param>
    /// <returns>The FNV-1a hash code of the substring beginning at <paramref name="start"/> and ending after <paramref name="length"/> characters.</returns>
    internal static int GetFNVHashCode(string text, int start, int length)
        => GetFNVHashCode(text.AsSpan(start, length));

    internal static int GetCaseInsensitiveFNVHashCode(string text)
        => GetCaseInsensitiveFNVHashCode(text.AsSpan());

    internal static int GetCaseInsensitiveFNVHashCode(ReadOnlySpan<char> data)
    {
        int hashCode = Hash.FnvOffsetBias;

        for (int i = 0; i < data.Length; i++)
        {
            hashCode = unchecked((hashCode ^ char.ToLowerInvariant(data[i])) * Hash.FnvPrime);
        }

        return hashCode;
    }

    /// <summary>
    /// Compute the hashcode of a string using FNV-1a
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="text">The input string</param>
    /// <returns>The FNV-1a hash code of <paramref name="text"/></returns>
    internal static int GetFNVHashCode(string text)
    {
        return CombineFNVHash(Hash.FnvOffsetBias, text);
    }

    /// <summary>
    /// Compute the hashcode of a string using FNV-1a
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="text">The input string</param>
    /// <returns>The FNV-1a hash code of <paramref name="text"/></returns>
    internal static int GetFNVHashCode(System.Text.StringBuilder text)
    {
        int hashCode = Hash.FnvOffsetBias;

#if NETCOREAPP3_1_OR_GREATER
            foreach (var chunk in text.GetChunks())
            {
                hashCode = CombineFNVHash(hashCode, chunk.Span);
            }
#else
        // StringBuilder.GetChunks is not available in this target framework. Since there is no other direct access
        // to the underlying storage spans of StringBuilder, we fall back to using slower per-character operations.
        int end = text.Length;

        for (int i = 0; i < end; i++)
        {
            hashCode = unchecked((hashCode ^ text[i]) * Hash.FnvPrime);
        }
#endif

        return hashCode;
    }

    /// <summary>
    /// Compute the hashcode of a sub string using FNV-1a
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="text">The input string as a char array</param>
    /// <param name="start">The start index of the first character to hash</param>
    /// <param name="length">The number of characters, beginning with <paramref name="start"/> to hash</param>
    /// <returns>The FNV-1a hash code of the substring beginning at <paramref name="start"/> and ending after <paramref name="length"/> characters.</returns>
    internal static int GetFNVHashCode(char[] text, int start, int length)
        => GetFNVHashCode(text.AsSpan(start, length));

    /// <summary>
    /// Compute the hashcode of a single character using the FNV-1a algorithm
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// Note: In general, this isn't any more useful than "char.GetHashCode". However,
    /// it may be needed if you need to generate the same hash code as a string or
    /// substring with just a single character.
    /// </summary>
    /// <param name="ch">The character to hash</param>
    /// <returns>The FNV-1a hash code of the character.</returns>
    internal static int GetFNVHashCode(char ch)
    {
        return Hash.CombineFNVHash(Hash.FnvOffsetBias, ch);
    }

    /// <summary>
    /// Combine a string with an existing FNV-1a hash code
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="hashCode">The accumulated hash code</param>
    /// <param name="text">The string to combine</param>
    /// <returns>The result of combining <paramref name="hashCode"/> with <paramref name="text"/> using the FNV-1a algorithm</returns>
    internal static int CombineFNVHash(int hashCode, string text)
        => CombineFNVHash(hashCode, text.AsSpan());

    /// <summary>
    /// Combine a char with an existing FNV-1a hash code
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="hashCode">The accumulated hash code</param>
    /// <param name="ch">The new character to combine</param>
    /// <returns>The result of combining <paramref name="hashCode"/> with <paramref name="ch"/> using the FNV-1a algorithm</returns>
    internal static int CombineFNVHash(int hashCode, char ch)
    {
        return unchecked((hashCode ^ ch) * Hash.FnvPrime);
    }

    /// <summary>
    /// Combine a string with an existing FNV-1a hash code
    /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
    /// </summary>
    /// <param name="hashCode">The accumulated hash code</param>
    /// <param name="data">The string to combine</param>
    /// <returns>The result of combining <paramref name="hashCode"/> with <paramref name="data"/> using the FNV-1a algorithm</returns>
    internal static int CombineFNVHash(int hashCode, ReadOnlySpan<char> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
        }

        return hashCode;
    }
}