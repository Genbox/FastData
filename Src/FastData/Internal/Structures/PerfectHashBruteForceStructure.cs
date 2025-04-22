using System.Diagnostics;
using System.Runtime.CompilerServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Structures;

internal sealed class PerfectHashBruteForceStructure : IHashStructure
{
    public bool TryCreate(object[] data, HashFunc hashFunc, out IContext? context)
    {
        uint localHash(object obj, uint seed) => Murmur_32(hashFunc(obj) ^ seed);

        long timestamp = Stopwatch.GetTimestamp();

        //Find the proper seeds
        uint[] seed = PerfectHashHelper.Generate(data, localHash, 1, uint.MaxValue, data.Length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 10;
        }).ToArray(); //We call .ToArray() as FirstOrDefault() would return 0 (in the default case), which is a valid seed.

        // If we have 0 seeds, it means either there is no solution, or we hit the exit condition
        if (seed.Length == 0)
        {
            context = null;
            return false;
        }

        KeyValuePair<object, uint>[] pairs = new KeyValuePair<object, uint>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            object value = data[i];

            uint hash = localHash(value, seed[0]);
            uint index = (uint)(hash % pairs.Length);
            pairs[index] = new KeyValuePair<object, uint>(value, hash);
        }

        context = new PerfectHashBruteForceContext(pairs, seed[0]);
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