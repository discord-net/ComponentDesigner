namespace Discord.CX;

public sealed record CreateGraphParameters(
    ICXModel CX,
    GraphOptions Options,
    ICompilationProvider CompilationProvider
);