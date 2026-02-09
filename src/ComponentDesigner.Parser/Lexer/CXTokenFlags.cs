using System;

namespace ComponentDesigner.Parser;

/// <summary>
///     An enum describing different statuses of a <see cref="CXToken"/>.
/// </summary>
[Flags]
public enum CXTokenFlags : byte
{
    /// <summary>
    ///     The token has no special status.
    /// </summary>
    None = 0,
    
    /// <summary>
    ///     The token is missing from the <see cref="CXSourceText"/>.
    /// </summary>
    Missing = 1 << 0,
    
    /// <summary>
    ///     The token was created synthetically and does not exist within the <see cref="CXSourceText"/>.
    /// </summary>
    Synthetic = 1 << 1,
}
