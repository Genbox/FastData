using System.Runtime.CompilerServices;
using Genbox.FastData.Enums;
using Genbox.FastData.Specs;

namespace Genbox.FastData;

public static class PrimitiveHash
{
    internal static HashFunc<T> GetHash<T>(DataType dataType) => dataType switch
    {
        DataType.Boolean => static obj => (uint)((bool)(object)obj ? 1 : 0),
        DataType.Char => static obj => (char)(object)obj,
        DataType.SByte => static obj => (uint)(sbyte)(object)obj,
        DataType.Byte => static obj => (byte)(object)obj,
        DataType.Int16 => static obj => (uint)(short)(object)obj,
        DataType.UInt16 => static obj => (ushort)(object)obj,
        DataType.Int32 => static obj => (uint)(int)(object)obj,
        DataType.UInt32 => static obj => (uint)(object)obj,
        DataType.Int64 => static obj => HashI64((long)(object)obj),
        DataType.UInt64 => static obj => HashU64((ulong)(object)obj),
        DataType.Single => static obj => HashF32((float)(object)obj),
        DataType.Double => static obj => HashF64((double)(object)obj),
        _ => throw new InvalidOperationException($"Unsupported data type: {dataType}")
    };

    private static uint HashI64(long value) => (uint)(value ^ (value >> 32));
    private static uint HashU64(ulong value) => (uint)(value ^ (value >> 32));

    private static uint HashF32(float value)
    {
        uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000)) >= 0x7FF0_0000)
            bits &= 0x7FF0_0000;

        return bits;
    }

    private static uint HashF64(double value)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
            bits &= 0x7FF0_0000_0000_0000;

        return (uint)bits ^ (uint)(bits >> 32);
    }
}