using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash;

namespace Genbox.FastData.Internal.Misc;

internal record HashData(ulong[] HashCodes, int CapacityFactor, bool HashCodesUnique, bool HashCodesPerfect)
{
    internal static HashData Create<T>(ReadOnlySpan<T> data, DataType dataType, int capacityFactor)
    {
        DefaultStringHash? stringHash = null;
        if (typeof(T) == typeof(string))
            stringHash = new DefaultStringHash();

        HashFunc<T> hashFunc;

        //If we have a string hash, use it.
        if (stringHash == null)
            hashFunc = PrimitiveHash.GetHash<T>(dataType);
        else
            hashFunc = (HashFunc<T>)(object)stringHash.GetHashFunction();

        ulong size = (ulong)(data.Length * capacityFactor);

        ulong[] hashCodes = new ulong[size];
        HashSet<ulong> uniqSet = new HashSet<ulong>();
        HashSet<ulong> perfectSet = new HashSet<ulong>(); //TOOD: Use direct addressing

        bool uniq = true;
        bool perfect = true;

        for (int i = 0; i < data.Length; i++)
        {
            ulong hash = hashFunc(data[i]);
            hashCodes[i] = hash;

            if (uniq && !uniqSet.Add(hash)) //The unique check is first so that when it is false, we don't try the other conditions
                uniq = false;

            if (perfect && !perfectSet.Add(hash % size))
                perfect = false;
        }

        return new HashData(hashCodes, capacityFactor, uniq, perfect);
    }
}