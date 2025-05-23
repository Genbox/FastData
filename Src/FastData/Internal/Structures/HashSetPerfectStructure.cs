using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetPerfectStructure<T> : IHashStructure<T>
{
    public bool TryCreate(T[] data, HashFunc<T> hashFunc, out IContext? context)
    {
        ulong[] hashCodes = new ulong[data.Length];

        for (int i = 0; i < data.Length; i++)
            hashCodes[i] = hashFunc(data[i]);

        //Find the proper seeds
        uint seed = PerfectHashHelper.Generate(hashCodes, static (hash, seed) => Mixers.Murmur_32((uint)(hash ^ seed)), 10_000_000);

        // If we have 0 seeds, it means either there is no solution, or we hit the exit condition
        if (seed == 0)
        {
            context = null;
            return false;
        }

        KeyValuePair<T, ulong>[] pairs = new KeyValuePair<T, ulong>[hashCodes.Length];

        for (int i = 0; i < hashCodes.Length; i++)
        {
            T value = data[i];

            uint hash = Mixers.Murmur_32((uint)(hashFunc(value) ^ seed));
            uint index = (uint)(hash % pairs.Length);
            pairs[index] = new KeyValuePair<T, ulong>(value, hash);
        }

        context = new HashSetPerfectContext<T>(pairs, seed);
        return true;
    }
}