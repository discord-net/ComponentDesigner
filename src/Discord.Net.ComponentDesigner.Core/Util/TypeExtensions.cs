namespace Discord.CX.Util;

public static class TypeExtensions
{
    extension(Type type)
    {
        public bool IsNumeric
            => type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(ushort) ||
               type == typeof(short) ||
               type == typeof(uint) ||
               type == typeof(int) ||
               type == typeof(ulong) ||
               type == typeof(long);
    }
}