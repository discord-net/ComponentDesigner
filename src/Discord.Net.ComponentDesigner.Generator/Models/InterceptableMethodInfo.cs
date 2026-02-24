using Microsoft.CodeAnalysis.CSharp;

namespace ComponentDesigner;

public sealed record InterceptableMethodInfo(
    InterceptableLocation Location,
    string ReturnType,
    string Parameters
)
{
}