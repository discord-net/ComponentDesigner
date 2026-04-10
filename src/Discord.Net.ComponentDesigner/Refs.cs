using System.Runtime.CompilerServices;

namespace Discord;

partial class ComponentDesigner
{
    public static RefBox<T> CreateRef<T>(out T value)
        where T : IMessageComponentBuilder
        => RefBox<T>.Create(out value);

    public static RefBox<IMessageComponentBuilder> CreateRef(out IMessageComponentBuilder value)
        => CreateRef<IMessageComponentBuilder>(out value);
}

public static class Ext
{
    extension<T>(T) where T : IMessageComponentBuilder
    {
        public static RefBox<T> CreateRef(out T value)
            => RefBox<T>.Create(out value);
    }
}

public readonly unsafe struct RefBox<T>
{
    public T Value
    {
        get => Ref;
        set => Ref = value;
    }

    private ref T Ref => ref Unsafe.AsRef<T>(_ptr);
    private readonly void* _ptr;

    public RefBox(void* ptr)
    {
        _ptr = ptr;
    }

    public static RefBox<T> Create(out T value)
    {
        value = default!;
        return new(Unsafe.AsPointer(ref value));
    }

    public U Set<U>(U value)
        where U : T
    {
        Value = value;
        return value;
    }
}