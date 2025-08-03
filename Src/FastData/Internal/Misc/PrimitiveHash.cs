using System.Runtime.CompilerServices;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Misc;

internal static class PrimitiveHash
{
    internal static HashFunc<T> GetHash<T>(KeyType keyType, bool hasZeroOrNaN) => keyType switch
    {
        KeyType.Char => static obj => (char)(object)obj!,
        KeyType.SByte => static obj => unchecked((ulong)(sbyte)(object)obj!),
        KeyType.Byte => static obj => (byte)(object)obj!,
        KeyType.Int16 => static obj => unchecked((ulong)(short)(object)obj!),
        KeyType.UInt16 => static obj => (ushort)(object)obj!,
        KeyType.Int32 => static obj => unchecked((ulong)(int)(object)obj!),
        KeyType.UInt32 => static obj => (uint)(object)obj!,
        KeyType.Single => obj => HashF32((float)(object)obj!, hasZeroOrNaN),
        KeyType.Int64 => static obj => unchecked((ulong)(long)(object)obj!), //Use value directly
        KeyType.UInt64 => static obj => (ulong)(object)obj!, //Use value directly
        KeyType.Double => obj => HashF64((double)(object)obj!, hasZeroOrNaN), //Does not fold to 32bit
        _ => throw new InvalidOperationException($"Unsupported data type: {keyType}")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashF32(float value, bool hasZeroOrNaN)
    {
        uint bits = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<float, byte>(ref value));

        unchecked
        {
            if (hasZeroOrNaN && ((bits - 1) & ~0x8000_0000) >= 0x7F80_0000)
                bits &= 0x7F80_0000;
        }

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashF64(double value, bool hasZeroOrNaN)
    {
        ulong bits = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<double, byte>(ref value));

        unchecked
        {
            if (hasZeroOrNaN && ((bits - 1) & ~(0x8000_0000_0000_0000)) >= 0x7FF0_0000_0000_0000)
                bits &= 0x7FF0_0000_0000_0000;
        }

        return bits;
    }
}