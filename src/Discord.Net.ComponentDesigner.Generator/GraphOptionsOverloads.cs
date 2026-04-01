using System.Collections.Generic;

namespace ComponentDesigner;

public readonly record struct GraphOptionsOverloads(
    Result<bool> EnableAutoRows,
    Result<bool> EnableAutoTextDisplays
)
{
    public bool IsEmpty => !EnableAutoRows.HasValue && !EnableAutoTextDisplays.HasValue;

    public IEnumerable<Diagnostic> Diagnostics
        => [..EnableAutoRows.Diagnostics, ..EnableAutoTextDisplays.Diagnostics];
}