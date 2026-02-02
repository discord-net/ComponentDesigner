namespace Discord.CX;

[Flags]
public enum SymbolModifiers
{
    Public = 1 << 0,
    Internal = 1 << 1,
    Private = 1 << 2,

    Static = 1 << 3,
    ReadOnly = 1 << 4
}