using System.Collections;
using System.Collections.Concurrent;
using Discord.CX.Util;

namespace Discord.CX;

public interface IDiagnosticBag
{
    void AddDiagnostics(Diagnostic diagnostic);
    void AddDiagnostics(params IEnumerable<Diagnostic> diagnostics);
}

public sealed class DiagnosticBag : IDiagnosticBag
{
    private List<Diagnostic>? _diagnostics;

    private DiagnosticBag()
    {
    }

    public static DiagnosticBag Get()
        => ObjectPool<DiagnosticBag>.Get(static () => new());

    public IReadOnlyList<Diagnostic> Use()
    {
        IReadOnlyList<Diagnostic> result = _diagnostics is null or { Count: 0 }
            ? []
            : [.._diagnostics];
        
        _diagnostics?.Clear();
        
        ObjectPool<DiagnosticBag>.Return(this);
        
        return result;
    }
    
    public void AddDiagnostics(Diagnostic diagnostic)
        => (_diagnostics ??= []).Add(diagnostic);

    public void AddDiagnostics(params IEnumerable<Diagnostic> diagnostics)
        => (_diagnostics ??= []).AddRange(diagnostics);
}