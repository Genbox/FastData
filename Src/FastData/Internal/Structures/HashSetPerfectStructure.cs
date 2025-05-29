using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class HashSetPerfectStructure<T> : IHashStructure<T>
{
    public bool TryCreate(T[] data, HashFunc<T> hashFunc, out IContext? context)
    {
        //This code is only called when the hash function is perfect.

        KeyValuePair<T, ulong>[] pairs = new KeyValuePair<T, ulong>[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            T key = data[i];
            pairs[i] = new KeyValuePair<T, ulong>(key, hashFunc(key));
        }

        context = new HashSetPerfectContext<T>(pairs);
        return true;
    }
}