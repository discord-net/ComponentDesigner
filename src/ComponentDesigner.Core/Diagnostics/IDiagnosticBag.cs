namespace ComponentDesigner;

public interface IDiagnosticBag
{
    bool HasAny { get; }
    
    bool HasErrors { get; }
    
    void Add(Diagnostic diagnostic);
    void Add(params IEnumerable<Diagnostic> diagnostics);

    IReadOnlyList<Diagnostic> ToCollection();
}

public sealed class PooledDiagnosticBag : IDiagnosticBag, IDisposable
{
    public bool HasAny => _diagnostics?.Count > 0;
    public bool HasErrors { get; private set; }
    
    private List<Diagnostic>? _diagnostics;

    private PooledDiagnosticBag()
    {
    }

    public static PooledDiagnosticBag Get(params ICollection<Diagnostic> initial)
    {
        var pooled = ObjectPool<PooledDiagnosticBag>.Get(static () => new());
        pooled._diagnostics?.Clear();
        pooled.HasErrors = false;

        if (initial.Count > 0) pooled.Add(initial);
        
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

    public void Dispose()
    {
        ObjectPool<PooledDiagnosticBag>.Return(this);
    }
}