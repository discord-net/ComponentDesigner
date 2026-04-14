using System.Diagnostics.CodeAnalysis;

namespace ComponentDesigner;

public static class ResultExtensions
{
    public static Result<T> Or<T>(this Result<T>? result, Result<T> other)
        where T : IEquatable<T>
        => result?.Or(other) ?? other;

    extension<T>(IEnumerable<Result<T>> collection)
    {
        public Result<IReadOnlyList<T>> FlattenAll()
        {
            var isAny = false;
            var isAll = true;
            var parts = new List<T>();
            var diag = new List<Diagnostic>();

            foreach (var result in collection)
            {
                isAny = true;
                if (result.HasValue) parts.Add(result.Value);
                else isAll = false;
                diag.AddRange(result.Diagnostics);
            }

            if (!isAny) return Result<IReadOnlyList<T>>.FromValue([]);

            return isAll ? new Result<IReadOnlyList<T>>([..parts], diag) : new(diag);
        }

        public Result<IReadOnlyList<T>> Flatten()
        {
            var parts = new List<T>();
            var diag = new List<Diagnostic>();

            foreach (var result in collection)
            {
                if (result.HasValue) parts.Add(result.Value);
                diag.AddRange(result.Diagnostics);
            }

            return new Result<IReadOnlyList<T>>([..parts], diag);
        }
    }

    extension<T>(Result<T> self)
    {
        public bool TryUnwrap(IDiagnosticBag bag, [MaybeNullWhen(false)] out T value)
        {
            bag.Add(self.Diagnostics);

            if (self.HasValue)
            {
                value = self.Value;
                return true;
            }

            value = default;
            return false;
        }
        
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? Unwrap(IDiagnosticBag bag, T? defaultValue = default)
        {
            bag.Add(self.Diagnostics);
            return self.GetValueOrDefault(defaultValue);
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? GetValueOrDefault(T? defaultValue = default)
            => self.HasValue ? self.Value : defaultValue;

        public Result<T> Or(Result<T> other)
            => self.HasValue ? self : other;

        public Result<U> Map<U>(Func<T, U> mapper)
            => self.HasValue ? new Result<U>(mapper(self.Value), self.Diagnostics) : new(self.Diagnostics);

        public Result<U> Map<U>(Func<T, Result<U>> mapper)
        {
            if (self.HasValue)
            {
                var mapped = mapper(self.Value);
                return new(
                    mapped.GetValueOrDefault()!,
                    mapped.HasValue,
                    [..self.Diagnostics, ..mapped.Diagnostics]
                );
            }

            return new(self.Diagnostics);
        }

        public Result<(T Left, U Right)> Combine<U>(Result<U> other)
            => self.Combine(other, (a, b) => (a, b));

        public Result<V> Combine<U, V>(Result<U> other, Func<T, U, V> mapper)
            => self.Combine(other, (a, b) => new Result<V>(mapper(a, b)));

        public Result<V> Combine<U, V>(Result<U> other, Func<T, U, Result<V>> mapper)
        {
            if (self.HasValue && other.HasValue)
            {
                var mapped = mapper(self.Value, other.Value);
                return new Result<V>(
                    mapped.GetValueOrDefault()!,
                    mapped.HasValue,
                    [..self.Diagnostics, ..other.Diagnostics, ..mapped.Diagnostics]
                );
            }

            return new Result<V>([..self.Diagnostics, ..other.Diagnostics]);
        }

        public static Result<T> FromValue(T value)
            => new(value);

        public static Result<T> FromValue(T value, params IReadOnlyList<Diagnostic> diagnostics)
            => new(value, diagnostics);

        public static Result<T> FromDiagnostics(
            params IReadOnlyList<Diagnostic> diagnostics
        ) => new(diagnostics);

        public Result<T> AddDiagnostics(IDiagnosticBag bag)
        {
            if (!bag.HasAny) return self;

            return self.AddDiagnostics(bag.ToCollection());
        }
    }
}