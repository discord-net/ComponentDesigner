namespace Discord.CX;

public readonly record struct DiagnosticDescriptor(
    string Id,
    DiagnosticSeverity Severity,
    string Title,
    string? Description = null
);