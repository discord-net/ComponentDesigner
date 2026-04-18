using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComponentDesigner;

public interface IRefBox : IDisposable;

public unsafe struct RefBox<T> : IRefBox
{
    public T Value
    {
        get => Ref;
        set => Ref = value;
    }

    private ref T Ref => ref Unsafe.AsRef<T>(_ptr);
    
    private readonly void* _ptr;
    private GCHandle _handle;
    
    public RefBox(ref T foo)
    {
        _ptr = Unsafe.AsPointer(ref foo);
        _handle = GCHandle.Alloc(foo);
    }
    
    public static RefBox<T> Create(out T value)
    {
        value = default!;
        return new(ref value);
    }

    public U Set<U>(U value)
        where U : T
    {
        Value = value;
        return value;
    }

    public void Dispose()
    {
        _handle.Free();
    }
}