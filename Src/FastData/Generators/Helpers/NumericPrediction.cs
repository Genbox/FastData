namespace Genbox.FastData.Generators.Helpers;

/// <summary>Generation-time integer coordinates and fixed-point coefficients for numeric prediction.</summary>
public static class NumericPrediction
{
    /// <summary>Maps a numeric key to an unsigned coordinate. Signed integers wrap; floating-point coordinates preserve order and canonicalize zero.</summary>
    public static ulong Coordinate(object value)
    {
        unchecked
        {
            if (value is float single)
            {
                uint bits = single == 0 ? 0 : BitConverter.ToUInt32(BitConverter.GetBytes(single), 0);
                return (bits & 0x80000000U) != 0 ? ~bits : bits ^ 0x80000000U;
            }
            if (value is double number)
            {
                ulong bits = number == 0 ? 0 : (ulong)BitConverter.DoubleToInt64Bits(number);
                return (bits & 0x8000000000000000UL) != 0 ? ~bits : bits ^ 0x8000000000000000UL;
            }
            return value switch
            {
                sbyte x => (ulong)x, short x => (ulong)x, int x => (ulong)x, long x => (ulong)x,
                byte x => x, ushort x => x, uint x => x, ulong x => x, char x => x,
                _ => throw new NotSupportedException("Unsupported numeric key type.")
            };
        }
    }

    /// <summary>Returns the fixed normalization shift for an interpolation range.</summary>
    public static int Shift(ulong range)
    {
        int shift = 0;
        while (range > uint.MaxValue)
        {
            range >>= 1;
            shift++;
        }
        return shift;
    }

    /// <summary>Encodes a slope in [0, 1] as a Q0.64 coefficient. One rounds down to the largest coefficient.</summary>
    public static ulong Coefficient(float slope) => slope >= 1 ? ulong.MaxValue : (ulong)(slope * 18446744073709551616d);

    internal static T FloatingKey<T>(ulong coordinate)
    {
        if (typeof(T) == typeof(float))
        {
            uint value = unchecked((uint)coordinate);
            uint bits = (value & 0x80000000U) != 0 ? value ^ 0x80000000U : ~value;
            return (T)(object)BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }
        ulong raw = (coordinate & 0x8000000000000000UL) != 0 ? coordinate ^ 0x8000000000000000UL : ~coordinate;
        return (T)(object)BitConverter.Int64BitsToDouble(unchecked((long)raw));
    }
}