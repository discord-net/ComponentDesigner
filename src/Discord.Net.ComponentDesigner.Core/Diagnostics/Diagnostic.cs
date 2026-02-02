namespace Discord.CX;

public readonly record struct Diagnostic(
    CXTextSpan TextSpan,
    DiagnosticDescriptor Descriptor
);