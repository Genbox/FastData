using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class BitSetStructure<TKey, TValue>(KeyProperties<TKey> props, KeyType keyType) : IStructure<TKey, TValue, BitSetContext<TKey, TValue>>
{
    public BitSetContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        int range = (int)props.Range + 1;
        ulong[] bitset = new ulong[(range + 63) / 64];
        TValue[]? denseValues = values == null ? null : new TValue[range];

        for (int i = 0; i < keys.Length; i++)
        {
            ulong offset = GetOffset(keyType, keys[i], props.MinKeyValue);
            int word = (int)(offset >> 6);
            bitset[word] |= 1UL << (int)(offset & 63);

            if (denseValues != null)
                denseValues[(int)offset] = values![i];
        }

        return new BitSetContext<TKey, TValue>(bitset, denseValues);
    }

    private static ulong GetOffset(KeyType keyType, TKey key, TKey min) => keyType switch
    {
        KeyType.Char => (char)(object)key - (uint)(char)(object)min,
        KeyType.SByte => (ulong)((long)(sbyte)(object)key - (sbyte)(object)min),
        KeyType.Byte => (ulong)((byte)(object)key - (byte)(object)min),
        KeyType.Int16 => (ulong)((long)(short)(object)key - (short)(object)min),
        KeyType.UInt16 => (ulong)((ushort)(object)key - (ushort)(object)min),
        KeyType.Int32 => (ulong)((long)(int)(object)key - (int)(object)min),
        KeyType.UInt32 => (uint)(object)key - (uint)(object)min,
        KeyType.Int64 => (ulong)((long)(object)key - (long)(object)min),
        KeyType.UInt64 => (ulong)(object)key - (ulong)(object)min,
        _ => throw new InvalidOperationException($"Unsupported key type: {keyType}")
    };
}