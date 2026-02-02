using System.Diagnostics.CodeAnalysis;

namespace Discord.CX;

public static class OptionalExtensions
{
    extension<T>(Optional<T> optional)
    {
        public bool TryGet([MaybeNullWhen(false)] out T value)
        {
            if (optional.IsSpecified)
            {
                value = optional.Value;
                return true;
            }

            value = default;
            return false;
        }
        
        public bool TryGetOfType<U>([MaybeNullWhen(false)] out U value)
            where U : T
        {
            if (optional is { IsSpecified: true, Value: U typed })
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}