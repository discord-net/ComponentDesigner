using System.Collections.Concurrent;

namespace ComponentDesigner;

public static class ResultBuilder
{
    extension<T>(Result<T>)
    {
        public static ResultBuilder<T> Builder => ResultBuilder<T>.Create();
    }
}

public sealed class ResultBuilder<T> : IDisposable, IDiagnosticBag
{
    private static readonly ConcurrentQueue<ResultBuilder<T>> Pool = new();

    public static ResultBuilder<T> Create()
        =>  Pool.TryDequeue(out var cached) ? cached : new ResultBuilder<T>();
    
    private List<Diagnostic>? _diagnostics;
    private T? _value;
    private bool _specified;
    private bool _hasErrors;

    public ResultBuilder<T> WithValue(T value)
    {
        _value = value;
        _specified = true;

        return this;
    }

    public ResultBuilder<T> AddDiagnostics(params IEnumerable<Diagnostic> diagnostics)
    {
        (_diagnostics ??= []).AddRange(diagnostics);
        return this;
    }
    
    public ResultBuilder<T> AddDiagnostic(Diagnostic diagnostic)
    {
        (_diagnostics ??= []).Add(diagnostic);
        _hasErrors |= diagnostic.Severity is DiagnosticSeverity.Error;
        return this;
    }

    public Result<T> Build() => new(
        _value!,
        _specified,
        _diagnostics?.Count > 0 ? [.._diagnostics] : []
    );
    
    public void Dispose()
    {
        _diagnostics?.Clear();
        _value = default;
        _specified = false;
        Pool.Enqueue(this);
    }

    void IDiagnosticBag.Add(Diagnostic diagnostic)
        => AddDiagnostic(diagnostic);
    
    void IDiagnosticBag.Add(params IEnumerable<Diagnostic> diagnostics)
        => AddDiagnostics(diagnostics);

    bool IDiagnosticBag.HasAny => _diagnostics?.Count > 0;
    bool IDiagnosticBag.HasErrors => _hasErrors;

    IReadOnlyList<Diagnostic> IDiagnosticBag.ToCollection() => _diagnostics?.ToArray() ?? [];
    int IDiagnosticBag.Count => _diagnostics?.Count ?? 0;

}