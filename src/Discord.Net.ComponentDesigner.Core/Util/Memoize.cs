using System.Collections.Concurrent;

namespace Discord.CX.Util;

public static class WeakMemoize
{
    private static readonly ConcurrentDictionary<int, WeakReference<object?>> _map = [];

    private static T GetOrAdd<T>(int key, Func<T> factory)
    {
        if (_map.TryGetValue(key, out var reference))
        {
            if (reference.TryGetTarget(out var target)) return (T)target!;

            _map.TryRemove(key, out _);
        }

        var value = factory();

        _map[key] = new(value);
        
        return value;
    }

    public static U Of<T, U>(T param, Func<T, U> factory)
        => GetOrAdd(
            Hash.Combine(typeof(T), typeof(U), param),
            () => factory(param)
        );
    
    public static V Of<T, U, V>(T param1, U param2, Func<T, U, V> factory)
        => GetOrAdd(
            Hash.Combine(typeof(T), typeof(U), typeof(V), param1, param2),
            () => factory(param1, param2)
        )!;
    
    public static W Of<T, U, V, W>(T param1, U param2, V param3, Func<T, U, V, W> factory)
        => GetOrAdd(
            Hash.Combine(typeof(T), typeof(U), typeof(V), typeof(W), param1, param2, param3),
            () => factory(param1, param2, param3)
        )!;
}

public static class Memoize
{
    private static readonly ConcurrentDictionary<int, object?> _map = [];

    public static U Of<T, U>(T param, Func<T, U> factory)
        => (U)_map.GetOrAdd(
            Hash.Combine(typeof(T), typeof(U), param),
            _ => factory(param)
        )!;
    
    public static V Of<T, U, V>(T param1, U param2, Func<T, U, V> factory)
        => (V)_map.GetOrAdd(
            Hash.Combine(typeof(T), typeof(U), typeof(V), param1, param2),
            _ => factory(param1, param2)
        )!;
    
    public static W Of<T, U, V, W>(T param1, U param2, V param3, Func<T, U, V, W> factory)
        => (W)_map.GetOrAdd(
            Hash.Combine(typeof(T), typeof(U), typeof(V), typeof(W), param1, param2, param3),
            _ => factory(param1, param2, param3)
        )!;
}