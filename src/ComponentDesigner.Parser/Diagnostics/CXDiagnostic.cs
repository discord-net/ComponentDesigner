using System;
using System.Linq;
using ComponentDesigner;

namespace ComponentDesigner.Parser;

/// <summary>
///     A data type describing a diagnostic emitted from the <see cref="CXParser"/>.
/// </summary>
/// <param name="Severity">The default severity of this diagnostic.</param>
/// <param name="Code">The unique code of this diagnostic.</param>
/// <param name="Message">A message describing the diagnostic in a human-readable way.</param>
public readonly record struct CXDiagnosticDescriptor(
    DiagnosticSeverity Severity,
    CXErrorCode Code,
    string Message
)
{
    /// <summary>
    ///     Constructs a new <see cref="CXDiagnosticDescriptor"/> for a missing elements closing tag.
    /// </summary>
    /// <param name="identifier">The elements identifier who is missing a closing tag.</param>
    public static CXDiagnosticDescriptor MissingElementClosingTag(CXIdentifier? identifier)
        => new(
            DiagnosticSeverity.Error,
            CXErrorCode.MissingElementClosingTag,
            identifier is not null
                ? $"Missing closing tag for '{identifier.Value}'"
                : "Missing fragment closing tag"
        );

    /// <summary>
    ///     Constructs a new <see cref="CXDiagnosticDescriptor"/> for an invalid root element.
    /// </summary>
    /// <param name="token">The token that caused this diagnostic to be produced.</param>
    public static CXDiagnosticDescriptor InvalidRootElement(CXToken token)
        => new(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidRootElement,
            $"'{token.Kind}' is not a valid root element"
        );

    /// <summary>
    ///     Constructs a new <see cref="CXDiagnosticDescriptor"/> for an invalid child of an element.
    /// </summary>
    /// <param name="token">The token that caused this diagnostic to be produced.</param>
    public static CXDiagnosticDescriptor InvalidElementChildToken(CXToken token)
        => new(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidElementChildToken,
            $"'{token.Kind}' is not a valid child of an element"
        );

    /// <summary>
    ///     Constructs a new <see cref="CXDiagnosticDescriptor"/> for an invalid string literal token.
    /// </summary>
    /// <param name="token">The token that caused this diagnostic to be produced.</param>
    public static CXDiagnosticDescriptor InvalidStringLiteralToken(CXToken token)
        => new(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidStringLiteralToken,
            $"'{token.Kind}' is not valid within a string literal"
        );

    /// <summary>
    ///     A <see cref="CXDiagnosticDescriptor"/> for an invalid attribute value.
    /// </summary>
    public static readonly CXDiagnosticDescriptor MissingAttributeValue = new(
            DiagnosticSeverity.Error,
            CXErrorCode.MissingAttributeValue,
            "Missing attribute value"
        );

    /// <summary>
    ///     Constructs a new <see cref="CXDiagnosticDescriptor"/> for an unexpected token.
    /// </summary>
    /// <param name="token">The token that caused this diagnostic to be produced.</param>
    /// <param name="message">The message to use for the diagnostic.</param>
    /// <param name="expected">The expected token kinds.</param>
    public static CXDiagnosticDescriptor UnexpectedToken(
        CXToken token,
        string? message = null,
        params CXTokenKind[] expected
    ) => new(
        DiagnosticSeverity.Error,
        CXErrorCode.UnexpectedToken,
        message ?? $"Unexpected token; expected {FormatExpected(expected)}, but got '{token.Kind}'"
    );

    /// <summary>
    ///     Formats an array of expected token kinds in a human-readable mannar.
    /// </summary>
    /// <param name="kinds">The different kinds of expected tokens.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="kinds"/> array was empty.</exception>
    private static string FormatExpected(CXTokenKind[] kinds)
    {
        if (kinds.Length is 0) throw new ArgumentOutOfRangeException(nameof(kinds));

        if (kinds.Length is 1) return $"'{kinds[0]}'";

        return
            $"one of {string.Join(", ", kinds.Take(kinds.Length - 1).Select(x => $"'{x}'"))} or '{kinds[kinds.Length - 1]}'";
    }
}

/// <summary>
///     A data type representing a diagnostic pointing to a specific location with a source. 
/// </summary>
/// <param name="Descriptor">The <see cref="CXDiagnosticDescriptor"/> describing the diagnostic.</param>
/// <param name="Span">The location within the source this diagnostic points to.</param>
public readonly record struct CXDiagnostic(
    CXDiagnosticDescriptor Descriptor,
    CXTextSpan Span
)
{
    /// <inheritdoc cref="CXDiagnosticDescriptor.Severity"/>
    public DiagnosticSeverity Severity => Descriptor.Severity;
    
    /// <inheritdoc cref="CXDiagnosticDescriptor.Code"/>
    public CXErrorCode Code => Descriptor.Code;
    
    /// <inheritdoc cref="CXDiagnosticDescriptor.Message"/>
    public string Message => Descriptor.Message;
}