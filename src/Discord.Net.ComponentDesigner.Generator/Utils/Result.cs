using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public readonly struct Result<T> : IEquatable<Result<T>>
    where T : IEquatable<T>?
{
    public static readonly Result<T> Empty = new(diagnostics: []);
    
    public T Value
    {
        get
        {
            if (HasResult) return _result!;

            throw new InvalidOperationException("Result doesn't have a value");
        }
    }

    public bool HasResult { get; }

    public EquatableArray<DiagnosticInfo> Diagnostics { get; }
    
    private readonly T? _result;

    private Result(T? value, bool hasValue, EquatableArray<DiagnosticInfo> diagnostics)
    {
        _result = value;
        HasResult = hasValue;
        Diagnostics = diagnostics;
    }
    
    public Result(T result, params IEnumerable<DiagnosticInfo> diagnostics) : this(result, [..diagnostics])
    {
    }
    
    public Result(T result, EquatableArray<DiagnosticInfo> diagnostics)
    {
        _result = result;
        HasResult = true;
        Diagnostics = [..diagnostics];
    }

    public Result(params IEnumerable<DiagnosticInfo> diagnostics) : this([..diagnostics])
    {
        
    }
    
    public Result(EquatableArray<DiagnosticInfo> diagnostics)
    {
        HasResult = false;
        Diagnostics = diagnostics;
    }

    public Result<T> AddDiagnostics(params ReadOnlySpan<DiagnosticInfo> diagnostics)
        => new(_result, HasResult, [..Diagnostics, ..diagnostics]);
    
    public Result<T> AddDiagnostics(DiagnosticInfo diagnostic)
        => new(_result, HasResult, [..Diagnostics, diagnostic]);

    public T? GetValueOrDefault(T? defaultValue = default)
        => HasResult ? Value : defaultValue;
    
    public Result<T> Or(Result<T> other)
        => HasResult ? this : other;
    
    public Result<U> Map<U>(Func<T, U> mapper) where U : IEquatable<U>
        => HasResult ? new Result<U>(mapper(Value), Diagnostics) : new Result<U>(Diagnostics);

    public Result<U> Map<U>(Func<T, Result<U>> mapper) where U : IEquatable<U>
    {
        if (HasResult)
        {
            var mapped = mapper(Value);
            return new Result<U>(
                mapped._result,
                mapped.HasResult,
                [..Diagnostics, ..mapped.Diagnostics]
            );
        }

        return new Result<U>(Diagnostics);;
    }

    public Result<(T Left, U Right)> Combine<U>(Result<U> other) where U : IEquatable<U>
        => HasResult && other.HasResult
            ? new Result<(T Left, U Right)>((Value, other.Value), [..Diagnostics, ..other.Diagnostics])
            : new([..Diagnostics, ..other.Diagnostics]);
    
    public Result<V> Combine<U, V>(Result<U> other, Func<T, U, V> mapper) 
        where U : IEquatable<U>
        where V : IEquatable<V>
        => HasResult && other.HasResult
            ? new Result<V>(mapper(Value, other.Value), [..Diagnostics, ..other.Diagnostics])
            : new([..Diagnostics, ..other.Diagnostics]);

    public Result<V> Combine<U, V>(Result<U> other, Func<T, U, Result<V>> mapper)
        where U : IEquatable<U>
        where V : IEquatable<V>
    {
        if (!HasResult || !other.HasResult)
            return new([..Diagnostics, ..other.Diagnostics]);

        var mapResult = mapper(Value, other.Value);

        return new(
            mapResult._result,
            mapResult.HasResult,
            [..Diagnostics, ..other.Diagnostics, ..mapResult.Diagnostics]
        );
    }
    
    public static Result<T> FromValue(
        T value,
        params IEnumerable<DiagnosticInfo> diagnostic
    ) => new(value, diagnostic);
    
    public static Result<T> FromValue(
        T value,
        DiagnosticDescriptor descriptor,
        TextSpan span
    ) => new(value, new DiagnosticInfo(descriptor, span));
    
    public static Result<T> FromValue(
        T value,
        DiagnosticDescriptor descriptor,
        ICXNode node
    ) => new(value, new DiagnosticInfo(descriptor, node.Span));

    public static Result<T> FromDiagnostics(
        params IEnumerable<DiagnosticInfo> diagnostic
    ) => new(diagnostic);
    
    public static Result<T> FromDiagnostics(
        params EquatableArray<DiagnosticInfo> diagnostic
    ) => new(diagnostic);

    public static Result<T> FromDiagnostic(
        DiagnosticDescriptor descriptor,
        TextSpan span
    ) => FromDiagnostics(new DiagnosticInfo(descriptor, span));
    
    public static Result<T> FromDiagnostic(
        DiagnosticDescriptor descriptor,
        ICXNode node
    ) => FromDiagnostics(new DiagnosticInfo(descriptor, node.Span));

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(DiagnosticInfo info) => FromDiagnostics(info);
    public static implicit operator Result<T>(EquatableArray<DiagnosticInfo> infos) => FromDiagnostics(infos);

    public static implicit operator Result<T>((T, IEnumerable<DiagnosticInfo>) value)
        => new(value.Item1, value.Item2);

    public bool Equals(Result<T> other)
    {
        return EqualityComparer<T?>.Default.Equals(_result, other._result) && 
               HasResult == other.HasResult && 
               Diagnostics.Equals(other.Diagnostics);
    }

    public override bool Equals(object? obj)
        => obj is Result<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EqualityComparer<T?>.Default.GetHashCode(_result);
            hashCode = (hashCode * 397) ^ HasResult.GetHashCode();
            hashCode = (hashCode * 397) ^ Diagnostics.GetHashCode();
            return hashCode;
        }
    }

    public sealed class Builder : IDiagnosticBag
    {
        private List<DiagnosticInfo>? _diag;
        private T? _value;
        private bool _hasValue;

        public Result<T> Build() => _hasValue
            ? new(_value!, _diag ?? [])
            : new Result<T>(_diag ?? []);

        public Builder WithValue(T value)
        {
            _value = value;
            _hasValue = true;

            return this;
        }

        public Builder AddDiagnostics(params IEnumerable<DiagnosticInfo> infos)
        {
            (_diag ??= []).AddRange(infos);
            return this;
        }
        
        public Builder AddDiagnostic(DiagnosticInfo info)
        {
            (_diag ??= []).Add(info);
            return this;
        }

        public static implicit operator Result<T>(Builder builder) => builder.Build();
        void IDiagnosticBag.AddDiagnostic(DiagnosticInfo info) => AddDiagnostic(info);
    } 
}

public static class ResultExtensions
{
    public static Result<T> Or<T>(this Result<T>? result, Result<T> other)
        where T : IEquatable<T>
        => result?.Or(other) ?? other;
    
    extension<T>(IEnumerable<Result<T>> collection) where T : IEquatable<T>
    {
        public Result<EquatableArray<T>> FlattenAll()
        {
            var isAll = true;
            var parts = new List<T>();
            var diag = new List<DiagnosticInfo>();

            foreach (var result in collection)
            {
                if (result.HasResult) parts.Add(result.Value);
                else isAll = false;
                diag.AddRange(result.Diagnostics);
            }

            return isAll ? new Result<EquatableArray<T>>([..parts], diag) : new(diag);
        }

        public Result<EquatableArray<T>> Flatten()
        {
            var parts = new List<T>();
            var diag = new List<DiagnosticInfo>();

            foreach (var result in collection)
            {
                if(result.HasResult) parts.Add(result.Value);
                diag.AddRange(result.Diagnostics);
            }

            return new Result<EquatableArray<T>>([..parts], diag);
        }
    }
}