using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ComponentDesigner;

// ReSharper disable InconsistentNaming

namespace Discord;

/// <summary>
///     Represents the entrypoint for creating components using the CX syntax.
/// </summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
public static partial class cx
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
{
    [DoesNotReturn]
    private static T Failed<T>()
        => throw new UnreachableException("Make sure interceptors are enabled for the component designer, if they are, this is a bug");

    public static CXMessageComponent message(
        [StringSyntax("html")] CXSyntax syntax,
        bool? autoRows = null,
        bool? autoTextDisplays = null
    ) => Failed<CXMessageComponent>();

    public static CXModalComponent modal(
        [StringSyntax("html")] CXSyntax syntax,
        bool? autoRows = null,
        bool? autoTextDisplays = null
    ) => Failed<CXModalComponent>();
    
    public static CXModalComponent any(
        [StringSyntax("html")] CXSyntax syntax,
        bool? autoRows = null,
        bool? autoTextDisplays = null
    ) => Failed<CXModalComponent>();
}
