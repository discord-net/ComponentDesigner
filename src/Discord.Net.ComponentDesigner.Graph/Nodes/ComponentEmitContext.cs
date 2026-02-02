using Discord.CX.Util;

namespace Discord.CX.Nodes;

public sealed class ComponentEmitContext : 
    IRendererContext,
    IEquatable<ComponentEmitContext>
{
    public ICompilationProvider CompilationProvider => _inner.CompilationProvider;

    public ICXModel CX => _inner.CX;
    
    public GraphOptions Options => _inner.Options;

    public IComponentRenderer Renderer { get; }
    
    private readonly IComponentContext _inner;

    private Dictionary<string, int>? _varsCount;
    
    public ComponentEmitContext(
        IComponentRenderer renderer,
        IComponentContext inner
    )
    {
        _inner = inner;
        Renderer = renderer;
    }

    public Result<string> Render(GraphNode node, ComponentOptions options = default, CancellationToken token = default)
        => node.Emit(this, options, token);

    public string CreateVariable(string hint = "local_")
    {
        _varsCount ??= [];
        
        if (!_varsCount.TryGetValue(hint, out var count))
            _varsCount[hint] = 1;
        else
            _varsCount[hint] = count + 1;

        return $"{hint}{count}";
    }

    public bool Equals(ComponentEmitContext? other)
        => other is not null &&
           ReferenceEquals(Renderer, other.Renderer) &&
           _inner.Equals(other._inner);

    public override bool Equals(object? obj)
        => obj is ComponentEmitContext other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(_inner, Renderer);
    
    bool IEquatable<IComponentContext>.Equals(IComponentContext? other)
        => other is ComponentEmitContext ctx && Equals(ctx);
}