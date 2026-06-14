namespace Genbox.FastData.Generators.Helpers;

public static class TypeHelper
{
    public static Type GetUnsignedType(Type type)
    {
        if (type == typeof(sbyte) || type == typeof(byte)) return typeof(byte);
        if (type == typeof(short) || type == typeof(ushort) || type == typeof(char)) return typeof(ushort);
        if (type == typeof(int) || type == typeof(uint)) return typeof(uint);
        if (type == typeof(long) || type == typeof(ulong)) return typeof(ulong);

        throw new InvalidOperationException($"Unsupported type: {type.Name}");
    }

    public static object ConvertValueToType(ulong value, Type type)
    {
        if (type == typeof(byte)) return unchecked((byte)value);
        if (type == typeof(ushort)) return unchecked((ushort)value);
        if (type == typeof(uint)) return unchecked((uint)value);
        if (type == typeof(ulong)) return value;

        throw new InvalidOperationException($"Unsupported type: {type.Name}");
    }
}