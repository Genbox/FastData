using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class KeyLengthCode : IStructure
{
    public IContext Create(object[] data)
    {
        //This implementation is the same as AutoUniqueLength, but takes duplicates into consideration

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        //Calculate the maximum length
        int maxLen = data.Cast<string>().Max(x => x.Length);

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        foreach (string value in data)
        {
            ref List<string>? item = ref lengths[value.Length];
            item ??= new List<string>();
            item.Add(value);
        }

        return new KeyLengthContext(data, lengths);
    }
}