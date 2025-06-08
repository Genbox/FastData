using System.Runtime.CompilerServices;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Internal.Misc;

internal static class PrimitiveHash
{
    internal static HashFunc<T> GetHash<T>(DataType dataType) => dataType switch
    {
        DataType.Boolean => static obj => (ulong)((bool)(object)obj! ? 1 : 0),
        DataType.Char => static obj => (char)(object)obj!,
        DataType.SByte => static obj => (ulong)(sbyte)(object)obj!,
        DataType.Byte => static obj => (byte)(object)obj!,
        DataType.Int16 => static obj => (ulong)(short)(object)obj!,
        DataType.UInt16 => static obj => (ushort)(object)obj!,
        DataType.Int32 => static obj => (ulong)(int)(object)obj!,
        DataType.UInt32 => static obj => (uint)(object)obj!,
        DataType.Single => static obj => HashF32((float)(object)obj!),
        DataType.Int64 => static obj => (ulong)(long)(object)obj!, //Use value directly
        DataType.UInt64 => static obj => (ulong)(object)obj!, //Use value directly
        DataType.Double => static obj => HashF64((double)(object)obj!), //Does not fold to 32bit
        _ => throw new InvalidOperationException($"Unsupported data type: {dataType}")
    };

    private static ulong HashF32(float value)
    {
        uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000)) >= 0x7FF0_0000)
            bits &= 0x7FF0_0000;

        return bits;
    }

    private static ulong HashF64(double value)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        if (((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
            bits &= 0x7FF0_0000_0000_0000;

        return bits;
    }
}