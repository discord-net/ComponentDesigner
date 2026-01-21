namespace Discord.CX;

public sealed record RenderedGraph(
    string Key,
    string CX,
    string? EmittedSource,
    EquatableArray<DiagnosticInfo> Diagnostics
);