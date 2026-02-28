namespace ComponentDesigner;

public enum TypeKind : byte
{
    Unknown = 0,
    Array = 1,
    Class = 2,
    Delegate = 3,
    Dynamic = 4,
    Enum = 5,
    Error = 6,
    Interface = 7,
    Module = 8,
    Pointer = 9,
    Struct = 10,
    TypeParameter = 11,
    Submission = 12,
    FunctionPointer = 13,
    Extension = 14,
}

public interface ICSharpTypeSymbol : ICSharpSymbol, IEquatable<ICSharpTypeSymbol>
{
    TypeKind TypeKind { get; }
    
    string Namespace { get; }
    
    ICSharpTypeSymbol? BaseType { get; }
    
    IReadOnlyList<ICSharpTypeSymbol> Interfaces { get; }
    
    IReadOnlyList<ICSharpTypeSymbol> TypeArguments { get; }
    
    ICSharpTypeSymbol? ConstructedFrom { get; }
    
    bool IsGeneric { get; }
    
    bool IsBoundGeneric { get; }
    
    bool IsValueType { get; }

    IReadOnlyList<ICSharpFieldSymbol> Fields { get; }

    ICSharpTypeSymbol ConstructGeneric(params IEnumerable<ICSharpTypeSymbol> typeArguments);
}