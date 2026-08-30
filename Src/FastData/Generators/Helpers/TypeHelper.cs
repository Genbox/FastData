namespace Genbox.FastData.Generators.Helpers;

/// <summary>Provides conversions used when mapping integral source types to generated storage types.</summary>
public static class TypeHelper
{
    /// <summary>Gets the unsigned storage type with the same width as a supported integral type.</summary>
    /// <param name="type">The source integral type.</param>
    /// <returns>The corresponding unsigned type.</returns>
    public static Type GetUnsignedType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type == typeof(sbyte) || type == typeof(byte)) return typeof(byte);
        if (type == typeof(short) || type == typeof(ushort) || type == typeof(char)) return typeof(ushort);
        if (type == typeof(int) || type == typeof(uint)) return typeof(uint);
        if (type == typeof(long) || type == typeof(ulong)) return typeof(ulong);

        throw new InvalidOperationException($"Unsupported type: {type.Name}");
    }

    /// <summary>Converts an unsigned value to a supported unsigned storage type.</summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="type">The destination type.</param>
    /// <returns>The converted boxed value.</returns>
    public static object ConvertValueToType(ulong value, Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type == typeof(byte)) return unchecked((byte)value);
        if (type == typeof(ushort)) return unchecked((ushort)value);
        if (type == typeof(uint)) return unchecked((uint)value);
        if (type == typeof(ulong)) return value;

        throw new InvalidOperationException($"Unsupported type: {type.Name}");
    }
}