namespace ComponentDesigner;

internal static class EquatableExtensions
{
    public static bool Equals<T>(this T? self, T? other) where T : class, IEquatable<T>
        => (self, other) switch
        {
            (not null, not null) => self.Equals(other),
            (null, null) => true,
            _ => false
        };
    
    public static bool Equals<T>(this T? self, T? other) where T : struct, IEquatable<T>
        => (self, other) switch
        {
            (not null, not null) => self.Value.Equals(other.Value),
            (null, null) => true,
            _ => false
        };
    
}