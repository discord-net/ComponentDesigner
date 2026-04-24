namespace ComponentDesigner;

public interface IDiagnosticBag
{
    int Count { get; }
    bool HasAny { get; }
    
    bool HasErrors { get; }
    
    void Add(Diagnostic diagnostic);
    void Add(params IEnumerable<Diagnostic> diagnostics);

    int Remove(DiagnosticDescriptor diagnostic);

    IReadOnlyList<Diagnostic> ToCollection();
}

public sealed class PooledDiagnosticBag : IDiagnosticBag, IDisposable
{
    public int Count => _diagnostics?.Count ?? 0;
    public bool HasAny => _diagnostics?.Count > 0;
    public bool HasErrors { get; private set; }
    
    private List<Diagnostic>? _diagnostics;

    private PooledDiagnosticBag()
    {
    }

    public static PooledDiagnosticBag Get(params IEnumerable<Diagnostic> initial)
    {
        var pooled = ObjectPool<PooledDiagnosticBag>.Get(static () => new());
        pooled._diagnostics?.Clear();
        pooled.HasErrors = false;

        pooled.Add(initial);
        
        return pooled;
    }
    

    public IReadOnlyList<Diagnostic> ToCollection()
    {
        IReadOnlyList<Diagnostic> result = _diagnostics is null or { Count: 0 }
            ? []
            : [.._diagnostics];
        
        _diagnostics?.Clear();
        
        return result;
    }

    public void Add(Diagnostic diagnostic)
    {
        (_diagnostics ??= []).Add(diagnostic);
        HasErrors |= diagnostic.Severity is DiagnosticSeverity.Error;
    }

    public void Add(params IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            Add(diagnostic);
        }
    }

    public int Remove(DiagnosticDescriptor descriptor)
    {
        if (_diagnostics is null) return 0;

        return _diagnostics.RemoveAll(x => x.Descriptor == descriptor);
    }

    public void Dispose()
    {
        ObjectPool<PooledDiagnosticBag>.Return(this);
    }
}