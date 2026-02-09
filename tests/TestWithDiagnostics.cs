using ComponentDesigner;
using ComponentDesigner.Parser;
using Xunit.Abstractions;

namespace UnitTests;

public abstract class TestWithDiagnostics(ITestOutputHelper output) : IDisposable
{
    private readonly Queue<Diagnostic> _diagnostics = [];
    private readonly HashSet<Diagnostic> _expectedDiagnostics = [];

    protected void ClearDiagnostics()
    {
        _diagnostics.Clear();
        _expectedDiagnostics.Clear();
        
    }

    protected void AssertEmptyDiagnostics()
    {
        Assert.Empty(_diagnostics);
    }

    protected void PushDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (!_expectedDiagnostics.Contains(diagnostic))
            {
                output.WriteLine($"{diagnostic.TextSpan}: [{diagnostic.Descriptor.Severity}]: {diagnostic.Descriptor.Title} - {diagnostic.Descriptor.Description}");
                _diagnostics.Enqueue(diagnostic);
            }
        }
    }
    
    protected Diagnostic AssertDiagnostic(
        DiagnosticDescriptor descriptor,
        ICXNode node
    ) => AssertDiagnostic(descriptor, node.Span);
    
    protected Diagnostic AssertDiagnostic(
        DiagnosticDescriptor descriptor,
        CXTextSpan? textSpan = null
    ) => AssertDiagnostic(
        descriptor.Id,
        descriptor.Title,
        descriptor.Description,
        descriptor.Severity,
        textSpan
    );

    protected Diagnostic AssertDiagnostic(
        string id,
        string? title = null,
        string? message = null,
        DiagnosticSeverity? severity = null,
        CXTextSpan? textSpan = null
    )
    {
        Assert.NotEmpty(_diagnostics);

        var diagnostic = _diagnostics.Dequeue();

        AssertDiagnostic(diagnostic, id, title, message, severity, textSpan);
        
        _expectedDiagnostics.Add(diagnostic);

        return diagnostic;
    }
    
    protected static Diagnostic AssertDiagnostic(
        Diagnostic diagnostic,
        DiagnosticDescriptor descriptor,
        ICXNode node
    ) => AssertDiagnostic(diagnostic, descriptor, node.Span);
    
    protected static Diagnostic AssertDiagnostic(
        Diagnostic diagnostic,
        DiagnosticDescriptor descriptor,
        CXTextSpan? span = null
    ) => AssertDiagnostic(
        diagnostic,
        descriptor.Id,
        descriptor.Title,
        descriptor.Description,
        descriptor.Severity,
        span
    );
    
    protected static Diagnostic AssertDiagnostic(
        Diagnostic diagnostic,
        string id,
        string? title = null,
        string? message = null,
        DiagnosticSeverity? severity = null,
        CXTextSpan? textSpan = null
    )
    {
        Assert.Equal(id, diagnostic.Id);
        
        if (title is not null) Assert.Equal(title, diagnostic.Title);
        if (message is not null) Assert.Equal(message, diagnostic.Description);
        if (severity is not null) Assert.Equal(severity, diagnostic.Severity);
        if (textSpan is not null) Assert.Equal(textSpan, diagnostic.TextSpan);

        return diagnostic;
    }
    
    protected virtual void EOF()
    {
        Assert.Empty(_diagnostics);
        _expectedDiagnostics.Clear();
    }

    public virtual void Dispose()
    {
        EOF();
    }
}