using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal;

/// <summary>Used internally in FastData to store hash codes and their properties.</summary>
internal record HashData(ulong[] HashCodes, float CapacityFactor, int TableSize, bool HashCodesUnique, bool HashCodesPerfect, ulong MinHashCode, ulong MaxHashCode)
{
    internal static HashData Create<T>(ReadOnlySpan<T> data, float capacityFactor, NumericHashFunc<T> func)
    {
        if (float.IsNaN(capacityFactor) || float.IsInfinity(capacityFactor) || capacityFactor <= 0)
            throw new InvalidOperationException("HashTableCapacityFactor must be a finite value greater than 0.");

        double tableSizeDouble = Math.Ceiling(data.Length * (double)capacityFactor);

        if (tableSizeDouble > int.MaxValue)
            throw new InvalidOperationException("HashTableCapacityFactor results in a hash table that is too large.");

        int tableSize = Math.Max(1, (int)tableSizeDouble);

        ulong[] hashCodes = new ulong[data.Length];
        HashSet<ulong> uniqSet = new HashSet<ulong>();
        SwitchingBitSet perfectTracker = new SwitchingBitSet(tableSize, false);

        bool uniq = true;
        bool perfect = true;
        ulong minHashCode = ulong.MaxValue;
        ulong maxHashCode = ulong.MinValue;

        for (int i = 0; i < data.Length; i++)
        {
            ulong hash = func(data[i]);
            hashCodes[i] = hash;

            minHashCode = Math.Min(minHashCode, hash);
            maxHashCode = Math.Max(maxHashCode, hash);

            if (uniq && !uniqSet.Add(hash)) //The unique check is first so that when it is false, we don't try the other conditions
                uniq = false;

            if (perfect && !perfectTracker.Add((uint)(hash % (uint)tableSize)))
                perfect = false;
        }

        return new HashData(hashCodes, capacityFactor, tableSize, uniq, perfect, minHashCode, maxHashCode);
    }
}