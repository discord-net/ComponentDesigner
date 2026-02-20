using System.Text;
using ComponentDesigner.Util;

namespace ComponentDesigner;

internal static class ObjectPoolExtensions
{
    extension<T>(ObjectPool<T>) where T : class, new()
    {
        public static T Get() => ObjectPool<T>.Get(() => new());

        public static ScopedResource GetScoped(out T result) 
            => ObjectPool<T>.GetScoped(() => new(), out result);
    }

    extension<T>(T) where T : class, new()
    {
        public static ScopedResource Pooled(out T instance)
            => ObjectPool<T>.GetScoped(out instance);
    }

    extension(StringBuilder)
    {
        public static ScopedResource Pooled(out StringBuilder instance)
        {
            var scope = ObjectPool<StringBuilder>.GetScoped(out instance);
            instance.Clear();
            return scope;
        }
    }
}