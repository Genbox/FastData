using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetPerfectStructure<T>(HashData hashData) : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        //This code is only called when the hash function is perfect.
        if (!hashData.HashCodesPerfect)
        {
            context = null;
            return false;
        }

        ulong size = (ulong)(data.Length * hashData.CapacityFactor);

        ulong[] hashCodes = hashData.HashCodes;
        KeyValuePair<T, ulong>[] pairs = new KeyValuePair<T, ulong>[size];

        //We need to reorder the data to match hashes
        for (int i = 0; i < data.Length; i++)
            pairs[hashCodes[i] % size] = new KeyValuePair<T, ulong>(data[i], hashCodes[i]);

        context = new HashSetPerfectContext<T>(pairs);
        return true;
    }
}