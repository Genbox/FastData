using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Misc;

/// <summary>Used internally in FastData to store hash codes and their properties.</summary>
internal record HashData(ulong[] HashCodes, int CapacityFactor, bool HashCodesUnique, bool HashCodesPerfect, ulong MinHashCode, ulong MaxHashCode)
{
    internal static HashData Create<T>(ReadOnlySpan<T> data, int capacityFactor, HashFunc<T> func)
    {
        if (capacityFactor <= 0)
            throw new InvalidOperationException("HashCapacityFactor must be greater than 0.");

        ulong size = checked((ulong)data.Length * (ulong)capacityFactor);

        if (size == 0)
            throw new InvalidOperationException("HashCapacityFactor results in zero-sized hash table.");

        if (size > int.MaxValue)
            throw new InvalidOperationException("HashCapacityFactor results in a hash table that is too large.");

        ulong[] hashCodes = new ulong[size];
        HashSet<ulong> uniqSet = new HashSet<ulong>();
        SwitchingBitSet perfectTracker = new SwitchingBitSet((int)size, false);

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

            if (perfect && !perfectTracker.Add((int)(hash % size)))
                perfect = false;
        }

        return new HashData(hashCodes, capacityFactor, uniq, perfect, minHashCode, maxHashCode);
    }
}