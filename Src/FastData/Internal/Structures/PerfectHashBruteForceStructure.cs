using System.Runtime.CompilerServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Structures;

internal sealed class PerfectHashBruteForceStructure<T> : IHashStructure<T>
{
    public bool TryCreate(T[] data, HashFunc<T> hashFunc, out IContext? context)
    {
        uint[] hashCodes = new uint[data.Length];

        for (int i = 0; i < data.Length; i++)
            hashCodes[i] = hashFunc(data[i]);

        //Find the proper seeds
        uint seed = PerfectHashHelper.Generate(hashCodes, static (hash, seed) => Murmur_32(hash) ^ seed, 10_000_000);

        // If we have 0 seeds, it means either there is no solution, or we hit the exit condition
        if (seed == 0)
        {
            context = null;
            return false;
        }

        KeyValuePair<T, uint>[] pairs = new KeyValuePair<T, uint>[hashCodes.Length];

        for (int i = 0; i < hashCodes.Length; i++)
        {
            T value = data[i];

            uint hash = Murmur_32(hashFunc(value) ^ seed);
            uint index = (uint)(hash % pairs.Length);
            pairs[index] = new KeyValuePair<T, uint>(value, hash);
        }

        context = new PerfectHashBruteForceContext<T>(pairs, seed);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Murmur_32(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x85EBCA6BU;
            h ^= h >> 13;
            h *= 0xC2B2AE35U;
            h ^= h >> 16;
            return h;
        }
    }
}