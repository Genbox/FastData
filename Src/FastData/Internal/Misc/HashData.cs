using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Misc;
using Genbox.FastData.StringHash;

namespace Genbox.FastData.Internal.Misc;

internal record HashData(ulong[] HashCodes, bool HashCodesUnique, bool HashCodesPerfect)
{
    internal static HashData Create<T>(T[] data, DataType dataType)
    {
        IStringHash? stringHash = null;
        if (data is string[])
            stringHash = /*analysisEnabled ? GetBestHash(stringArr, props.StringProps!, fdCfg.SimulatorConfig, factory) :*/ new DefaultStringHash();

        HashFunc<T> hashFunc;

        //If we have a string hash, use it.
        if (stringHash == null)
            hashFunc = PrimitiveHash.GetHash<T>(dataType);
        else
            hashFunc = (HashFunc<T>)(object)stringHash.GetHashFunction();

        ulong[] hashCodes = new ulong[data.Length];
        HashSet<ulong> uniqSet = new HashSet<ulong>();
        HashSet<ulong> perfectSet = new HashSet<ulong>(); //TOOD: Use direct addressing

        bool uniq = true;
        bool perfect = true;

        ulong length = (ulong)data.Length;
        for (int i = 0; i < data.Length; i++)
        {
            ulong hash = hashFunc(data[i]);
            hashCodes[i] = hash;

            if (uniq && !uniqSet.Add(hash)) //The unique check is first so that when it is false, we don't try the other conditions
                uniq = false;

            if (perfect && !perfectSet.Add(hash % length))
                perfect = false;
        }

        return new HashData(hashCodes, uniq, perfect);
    }
}