namespace Discord;

/// <summary>
///     Marks this functional components parameter to accept children of the element. 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CXChildrenAttribute : Attribute;