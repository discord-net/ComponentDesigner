using System.Buffers;
using System.Runtime.CompilerServices;

namespace Discord;

/// <summary>
///     A <c>ref struct</c> containing interpolated values within the CX syntax.
/// </summary>
[InterpolatedStringHandler]
public ref struct DesignerInterpolationHandler : IDisposable
{
    // incrementing index into the '_interpolatedValues'
    private int _index;
    
    // the values we'll want to extract later
    private readonly object?[]? _interpolatedValues;

    /// <summary>
    ///     Constructs a new <see cref="DesignerInterpolationHandler"/>.
    /// </summary>
    /// <param name="literalLength">The number of literal values.</param>
    /// <param name="formattedCount">The number of interpolated values.</param>
    public DesignerInterpolationHandler(
        int literalLength,
        int formattedCount
    )
    {
        if (formattedCount > 0)
            _interpolatedValues = ArrayPool<object?>.Shared.Rent(formattedCount);
    }

    /// <summary>
    ///     Appends a literal value.
    /// </summary>
    /// <param name="value">The literal value to append.</param>
    /// <remarks>
    ///     This has no behavior for the component designer implementation.
    /// </remarks>
    public void AppendLiteral(string value)
    {
    }

    /// <summary>
    ///     Adds an interpolated value to the designer.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <typeparam name="T">The type of the value to add.</typeparam>
    /// <exception cref="IndexOutOfRangeException">The designer is full.</exception>
    public void AppendFormatted<T>(T value)
        => _interpolatedValues?[_index++] = value;

    /// <summary>
    ///     Gets an interpolated value from the designer.
    /// </summary>
    /// <param name="index">The index of the value to get.</param>
    /// <returns>The value at the specified index.</returns>
    public object? GetValue(int index) => _interpolatedValues?[index];
    
    /// <summary>
    ///     Gets an interpolated value from the designer of the given type.
    /// </summary>
    /// <param name="index">The index of the value to get.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The value at the specified index.</returns>
    /// <remarks>
    ///     If the type <typeparamref name="T"/> doesn't match the value at the given index, the
    ///     <see langword="default"/>(<typeparamref name="T"/>) is returned. 
    /// </remarks>
    public T? GetValue<T>(int index) => (T?)_interpolatedValues?[index];
    
    /// <summary>
    ///     Gets an interpolated value from the designer and converts it to a string using its <c>ToString</c> function.
    /// </summary>
    /// <param name="index">The index of the value to get.</param>
    /// <returns>The string representation of the value at the specified index.</returns>
    public string? GetValueAsString(int index) => _interpolatedValues?[index]?.ToString();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_interpolatedValues is not null)
        {
            for (var i = 0; i < _interpolatedValues.Length; i++)
            {
                if(_interpolatedValues[i] is IRefBox refBox)
                    refBox.Dispose();
            }
            
            ArrayPool<object?>.Shared.Return(_interpolatedValues);
        }
    }
}