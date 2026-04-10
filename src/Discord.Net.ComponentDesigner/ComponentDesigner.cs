using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable InconsistentNaming

namespace Discord;

/// <summary>
///     Represents the entrypoint for creating components using the CX syntax.
/// </summary>
public static partial class ComponentDesigner
{
    /// <summary>
    ///     Returns the pre-compiled components representing the provided CX syntax.
    /// </summary>
    /// <param name="designer">The CX syntax containing string interpolations.</param>
    /// <param name="autoRows">
    ///     Whether to support
    ///     <a href="https://github.com/discord-net/ComponentDesigner?tab=readme-ov-file#auto-rows">auto rows</a> within
    ///     the CX syntax.
    ///     </param>
    /// <param name="autoTextDisplays">
    ///     Whether to support
    ///     <a href="https://github.com/discord-net/ComponentDesigner?tab=readme-ov-file#auto-text-display">
    ///         auto text displays
    ///     </a>
    ///     within the CX syntax.
    /// </param>
    /// <returns>The pre-compiled <see cref="CXMessageComponent"/> representing the CX syntax.</returns>
    /// <exception cref="UnreachableException">
    ///     The generator failed to produce a valid interceptor.
    /// </exception>
    public static CXMessageComponent cx(
        [StringSyntax("html")] DesignerInterpolationHandler designer,
        bool? autoRows = null,
        bool? autoTextDisplays = null
    ) => throw new UnreachableException("Make sure interceptors are enabled for the component designer, if they are, this is a bug");

    /// <summary>
    ///     Returns the pre-compiled components representing the provided CX syntax.
    /// </summary>
    /// <param name="cx">The CX syntax.</param>
    /// <param name="autoRows">
    ///     Whether to support
    ///     <a href="https://github.com/discord-net/ComponentDesigner?tab=readme-ov-file#auto-rows">auto rows</a> within
    ///     the CX syntax.
    ///     </param>
    /// <param name="autoTextDisplays">
    ///     Whether to support
    ///     <a href="https://github.com/discord-net/ComponentDesigner?tab=readme-ov-file#auto-text-display">
    ///         auto text displays
    ///     </a>
    ///     within the CX syntax.
    /// </param>
    /// <returns>The pre-compiled <see cref="CXMessageComponent"/> representing the CX syntax.</returns>
    /// <exception cref="UnreachableException">
    ///     The generator failed to produce a valid interceptor.
    /// </exception>
    public static CXMessageComponent cx(
        [StringSyntax("html")] string cx,
        bool? autoRows = null,
        bool? autoTextDisplays = null
    ) => throw new UnreachableException("Make sure interceptors are enabled for the component designer, if they are, this is a bug");
}
