namespace Genbox.FastData.Generators.Extensions;

/// <summary>Provides extension methods for the <see cref="Type" /> class.</summary>
public static class TypeCodeExtensions
{
    /// <summary>Determines whether the specified <see cref="Type" /> represents a numeric type.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type is a numeric type; otherwise, <see langword="false" />.</returns>
    public static bool IsNumeric(this TypeCode type) => type switch
    {
        TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Single or TypeCode.Double or TypeCode.UInt32 or TypeCode.UInt16 or TypeCode.UInt64 or TypeCode.Byte or TypeCode.Char => true,
        TypeCode.String => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static byte GetBitWidth(this TypeCode type) => type switch
    {
        TypeCode.SByte or TypeCode.Byte => 8,
        TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Char => 16,
        TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 32,
        TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Determines whether the specified <see cref="Type" /> represents a numeric type.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type is a numeric type; otherwise, <see langword="false" />.</returns>
    public static bool IsIntegral(this TypeCode type) => type switch
    {
        TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.UInt32 or TypeCode.UInt16 or TypeCode.UInt64 or TypeCode.Byte or TypeCode.Char => true,
        TypeCode.String or TypeCode.Single or TypeCode.Double => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Determines whether the specified <see cref="Type" /> uses identity hashing.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type uses identity hashing; otherwise, <see langword="false" />.</returns>
    public static bool UsesIdentityHash(this TypeCode type) => type switch
    {
        TypeCode.Char or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => true,
        TypeCode.String or TypeCode.Single or TypeCode.Double => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static TKey GetMinValue<TKey>(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Char => (TKey)(object)char.MinValue,
        TypeCode.SByte => (TKey)(object)sbyte.MinValue,
        TypeCode.Byte => (TKey)(object)byte.MinValue,
        TypeCode.Int16 => (TKey)(object)short.MinValue,
        TypeCode.UInt16 => (TKey)(object)ushort.MinValue,
        TypeCode.Int32 => (TKey)(object)int.MinValue,
        TypeCode.UInt32 => (TKey)(object)uint.MinValue,
        TypeCode.Int64 => (TKey)(object)long.MinValue,
        TypeCode.UInt64 => (TKey)(object)ulong.MinValue,
        TypeCode.Single => (TKey)(object)float.MinValue,
        TypeCode.Double => (TKey)(object)double.MinValue,
        _ => throw new InvalidOperationException($"Unsupported numeric type: {typeof(TKey)}")
    };

    public static TKey GetMaxValue<TKey>(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Char => (TKey)(object)char.MaxValue,
        TypeCode.SByte => (TKey)(object)sbyte.MaxValue,
        TypeCode.Byte => (TKey)(object)byte.MaxValue,
        TypeCode.Int16 => (TKey)(object)short.MaxValue,
        TypeCode.UInt16 => (TKey)(object)ushort.MaxValue,
        TypeCode.Int32 => (TKey)(object)int.MaxValue,
        TypeCode.UInt32 => (TKey)(object)uint.MaxValue,
        TypeCode.Int64 => (TKey)(object)long.MaxValue,
        TypeCode.UInt64 => (TKey)(object)ulong.MaxValue,
        TypeCode.Single => (TKey)(object)float.MaxValue,
        TypeCode.Double => (TKey)(object)double.MaxValue,
        _ => throw new InvalidOperationException($"Unsupported numeric type: {typeof(TKey)}")
    };
}