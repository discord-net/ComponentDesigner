namespace Discord.CX.Util;

public readonly ref struct ScopedResource(Action callback) : IDisposable
{
    public void Dispose() => callback();
}