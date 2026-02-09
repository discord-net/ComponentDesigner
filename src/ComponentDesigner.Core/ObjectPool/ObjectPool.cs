using ComponentDesigner.Util;

namespace ComponentDesigner;

internal static class ObjectPool<T>
    where T : class
{
    public static int DemandCapacity { get; } = 5;
    public static int ReserveCapacity { get; } = 32;

    public static int AvailableOnDemand => Demand.Count;
    public static int AvailableInReserve => Reserve.Count;

    public static int TotalAvailable => AvailableInReserve + AvailableOnDemand;
    
    private static readonly Queue<T> Demand = new();
    private static readonly Queue<WeakReference<T>> Reserve = new();

    private static readonly object _lock = new();

    public static ScopedResource GetScoped(Func<T> factory, out T value)
    {
        var local = value = Get(factory);
        return new(() => Return(local));
    }
    
    public static T Get(Func<T> factory)
    {
        lock (_lock)
        {
            if (Demand.Count > 0) return Demand.Dequeue();

            while (Reserve.Count > 0)
            {
                var reference = Reserve.Dequeue();

                if (reference.TryGetTarget(out var target)) return target;
            }

            return factory();
        }
    }

    public static void Return(T value)
    {
        lock (_lock)
        {
            if(Demand.Count < DemandCapacity)
                Demand.Enqueue(value);
            else if(Reserve.Count < ReserveCapacity)
                Reserve.Enqueue(new(value));
        }
    }
}