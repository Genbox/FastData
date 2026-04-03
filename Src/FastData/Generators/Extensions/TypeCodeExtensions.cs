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

    public static bool IsFloatingPoint(this TypeCode type) => type switch
    {
        TypeCode.Single or TypeCode.Double => true,
        TypeCode.String or TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.UInt32 or TypeCode.UInt16 or TypeCode.UInt64 or TypeCode.Byte or TypeCode.Char => false,
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

    public static Func<ulong, TKey> GetUnsignedKeyConverter<TKey>(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Byte => static value => (TKey)(object)(byte)value,
        TypeCode.Char => static value => (TKey)(object)(char)value,
        TypeCode.UInt16 => static value => (TKey)(object)(ushort)value,
        TypeCode.UInt32 => static value => (TKey)(object)(uint)value,
        TypeCode.UInt64 => static value => (TKey)(object)value,
        _ => throw new InvalidOperationException($"Unsupported unsigned type: {typeof(TKey)}")
    };

    public static Func<long, TKey> GetSignedKeyConverter<TKey>(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.SByte => static value => (TKey)(object)(sbyte)value,
        TypeCode.Int16 => static value => (TKey)(object)(short)value,
        TypeCode.Int32 => static value => (TKey)(object)(int)value,
        TypeCode.Int64 => static value => (TKey)(object)value,
        _ => throw new InvalidOperationException($"Unsupported signed type: {typeof(TKey)}")
    };

    public static Func<TKey, long> GetSignedValueConverter<TKey>(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Char => static value => (char)(object)value!,
        TypeCode.SByte => static value => (sbyte)(object)value!,
        TypeCode.Byte => static value => (byte)(object)value!,
        TypeCode.Int16 => static value => (short)(object)value!,
        TypeCode.UInt16 => static value => (ushort)(object)value!,
        TypeCode.Int32 => static value => (int)(object)value!,
        TypeCode.UInt32 => static value => (uint)(object)value!,
        TypeCode.Int64 => static value => (long)(object)value!,
        TypeCode.UInt64 => static value => unchecked((long)(ulong)(object)value!),
        TypeCode.Single => static value => (long)(float)(object)value!,
        TypeCode.Double => static value => (long)(double)(object)value!,
        _ => throw new InvalidOperationException($"Unsupported signed value type: {typeof(TKey)}")
    };

    public static Func<TKey, ulong> GetUnsignedValueConverter<TKey>(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Char => static value => (char)(object)value!,
        TypeCode.SByte => static value => unchecked((ulong)(sbyte)(object)value!),
        TypeCode.Byte => static value => (byte)(object)value!,
        TypeCode.Int16 => static value => unchecked((ulong)(short)(object)value!),
        TypeCode.UInt16 => static value => (ushort)(object)value!,
        TypeCode.Int32 => static value => unchecked((ulong)(int)(object)value!),
        TypeCode.UInt32 => static value => (uint)(object)value!,
        TypeCode.Int64 => static value => unchecked((ulong)(long)(object)value!),
        TypeCode.UInt64 => static value => (ulong)(object)value!,
        TypeCode.Single => static value => (ulong)(long)(float)(object)value!,
        TypeCode.Double => static value => (ulong)(long)(double)(object)value!,
        _ => throw new InvalidOperationException($"Unsupported unsigned value type: {typeof(TKey)}")
    };

    public static bool IsUnsigned(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Char => true,
        TypeCode.SByte => false,
        TypeCode.Byte => true,
        TypeCode.Int16 => false,
        TypeCode.UInt16 => true,
        TypeCode.Int32 => false,
        TypeCode.UInt32 => true,
        TypeCode.Int64 => false,
        TypeCode.UInt64 => true,
        TypeCode.Single => false,
        TypeCode.Double => false,
        _ => throw new InvalidOperationException($"Unsupported type: {typeCode}")
    };
}