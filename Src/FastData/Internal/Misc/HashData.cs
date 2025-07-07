using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Internal.Misc;

/// <summary>Used internally in FastData to store hash codes and their properties.</summary>
internal record HashData(ulong[] HashCodes, int CapacityFactor, bool HashCodesUnique, bool HashCodesPerfect)
{
    internal static HashData Create<T>(ReadOnlySpan<T> data, int capacityFactor, HashFunc<T> func)
    {
        ulong size = (ulong)(data.Length * capacityFactor);

        ulong[] hashCodes = new ulong[size];
        HashSet<ulong> uniqSet = new HashSet<ulong>();
        HashSet<ulong> perfectSet = new HashSet<ulong>(); //TODO: Use direct addressing

        bool uniq = true;
        bool perfect = true;

        for (int i = 0; i < data.Length; i++)
        {
            ulong hash = func(data[i]);
            hashCodes[i] = hash;

            if (uniq && !uniqSet.Add(hash)) //The unique check is first so that when it is false, we don't try the other conditions
                uniq = false;

            if (perfect && !perfectSet.Add(hash % size))
                perfect = false;
        }

        return new HashData(hashCodes, capacityFactor, uniq, perfect);
    }
}