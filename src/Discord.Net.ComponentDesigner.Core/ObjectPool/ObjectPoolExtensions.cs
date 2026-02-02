using System.Text;
using Discord.CX.Util;

namespace Discord.CX;

internal static class ObjectPoolExtensions
{
    extension<T>(ObjectPool<T>) where T : class, new()
    {
        public static T Get() => ObjectPool<T>.Get(() => new());

        public static ScopedResource GetScoped(out T result) 
            => ObjectPool<T>.GetScoped(() => new(), out result);
    }
}