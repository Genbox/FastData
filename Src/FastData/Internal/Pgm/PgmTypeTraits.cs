using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Convert = System.Convert;

namespace Genbox.FastData.Internal.Pgm;

internal static class PgmTypeTraits<T> where T : notnull
{
    public static readonly bool IsFloatingPoint = typeof(T) == typeof(float) || typeof(T) == typeof(double);
    public static readonly bool IsSignedInteger = typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long);
    public static readonly bool IsUnsignedInteger = typeof(T) == typeof(byte) || typeof(T) == typeof(char) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong);

    public static T MaxValue
    {
        get
        {
            if (typeof(T) == typeof(float)) return (T)(object)float.PositiveInfinity;
            if (typeof(T) == typeof(double)) return (T)(object)double.PositiveInfinity;
            if (typeof(T) == typeof(byte)) return (T)(object)byte.MaxValue;
            if (typeof(T) == typeof(sbyte)) return (T)(object)sbyte.MaxValue;
            if (typeof(T) == typeof(char)) return (T)(object)char.MaxValue;
            if (typeof(T) == typeof(short)) return (T)(object)short.MaxValue;
            if (typeof(T) == typeof(ushort)) return (T)(object)ushort.MaxValue;
            if (typeof(T) == typeof(int)) return (T)(object)int.MaxValue;
            if (typeof(T) == typeof(uint)) return (T)(object)uint.MaxValue;
            if (typeof(T) == typeof(long)) return (T)(object)long.MaxValue;
            if (typeof(T) == typeof(ulong)) return (T)(object)ulong.MaxValue;
            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }
    }

    public static double ToDouble(T value)
    {
        if (typeof(T) == typeof(float)) return (float)(object)value;
        if (typeof(T) == typeof(double)) return (double)(object)value;
        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    public static long ToInt64(T value)
    {
        if (typeof(T) == typeof(sbyte)) return (sbyte)(object)value;
        if (typeof(T) == typeof(short)) return (short)(object)value;
        if (typeof(T) == typeof(int)) return (int)(object)value;
        if (typeof(T) == typeof(long)) return (long)(object)value;
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    public static ulong ToUInt64(T value)
    {
        if (typeof(T) == typeof(byte)) return (byte)(object)value;
        if (typeof(T) == typeof(char)) return (char)(object)value;
        if (typeof(T) == typeof(ushort)) return (ushort)(object)value;
        if (typeof(T) == typeof(uint)) return (uint)(object)value;
        if (typeof(T) == typeof(ulong)) return (ulong)(object)value;
        return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
    }

    public static Int128 ToInt128(T value)
    {
        if (IsSignedInteger)
            return ToInt64(value);
        if (IsUnsignedInteger)
            return ToUInt64(value);
        if (IsFloatingPoint)
            return (Int128)ToDouble(value);
        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    public static T FromInt128(Int128 value)
    {
        if (typeof(T) == typeof(sbyte)) return (T)(object)(sbyte)value;
        if (typeof(T) == typeof(short)) return (T)(object)(short)value;
        if (typeof(T) == typeof(int)) return (T)(object)(int)value;
        if (typeof(T) == typeof(long)) return (T)(object)(long)value;
        if (typeof(T) == typeof(byte)) return (T)(object)(byte)value;
        if (typeof(T) == typeof(char)) return (T)(object)(char)value;
        if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)value;
        if (typeof(T) == typeof(uint)) return (T)(object)(uint)value;
        if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)value;
        if (typeof(T) == typeof(float)) return (T)(object)(float)value;
        if (typeof(T) == typeof(double)) return (T)(object)(double)value;
        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    public static T FromDouble(double value)
    {
        if (typeof(T) == typeof(float)) return (T)(object)(float)value;
        if (typeof(T) == typeof(double)) return (T)(object)value;
        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }

    public static T AddOne(T a)
    {
        unchecked
        {
            if (typeof(T) == typeof(byte)) return (T)(object)(byte)((byte)(object)a + 1);
            if (typeof(T) == typeof(sbyte)) return (T)(object)(sbyte)((sbyte)(object)a + 1);
            if (typeof(T) == typeof(char)) return (T)(object)(char)((char)(object)a + 1);
            if (typeof(T) == typeof(short)) return (T)(object)(short)((short)(object)a + 1);
            if (typeof(T) == typeof(ushort)) return (T)(object)(ushort)((ushort)(object)a + 1);
            if (typeof(T) == typeof(int)) return (T)(object)((int)(object)a + 1);
            if (typeof(T) == typeof(uint)) return (T)(object)((uint)(object)a + 1);
            if (typeof(T) == typeof(long)) return (T)(object)((long)(object)a + 1);
            if (typeof(T) == typeof(ulong)) return (T)(object)((ulong)(object)a + 1);
            if (typeof(T) == typeof(float)) return (T)(object)((float)(object)a + 1f);
            if (typeof(T) == typeof(double)) return (T)(object)((double)(object)a + 1d);
            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }
    }

    [SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
    public static T NextAfter(T value)
    {
        if (typeof(T) == typeof(float))
#if NETSTANDARD2_0
            return (T)(object)MathCompat.BitIncrement((float)(object)value);
#else
            return (T)(object)MathF.BitIncrement((float)(object)value);
#endif
        if (typeof(T) == typeof(double))
#if NETSTANDARD2_0
            return (T)(object)MathCompat.BitIncrement((double)(object)value);
#else
            return (T)(object)Math.BitIncrement((double)(object)value);
#endif
        return AddOne(value);
    }

    /// <summary>Returns the previous representable value before the given value. For floating-point types, returns the next smaller representable value. For integer types, returns value - 1.</summary>
    public static T PreviousValue(T value)
    {
        if (typeof(T) == typeof(float))
#if NETSTANDARD2_0
            return (T)(object)MathCompat.BitDecrement((float)(object)value);
#else
            return (T)(object)MathF.BitDecrement((float)(object)value);
#endif
        if (typeof(T) == typeof(double))
#if NETSTANDARD2_0
            return (T)(object)MathCompat.BitDecrement((double)(object)value);
#else
            return (T)(object)Math.BitDecrement((double)(object)value);
#endif
        throw new NotSupportedException("PreviousValue is only supported for floating-point types.");
    }
}