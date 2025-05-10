using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class KeyLengthStructure<T>(StructureConfig<T> config) : IStructure<T>
{
    public bool TryCreate(T[] data, out IContext? context)
    {
        // This data structure is only appropriate for strings
        if (data is not string[] stringArr)
        {
            context = null;
            return false;
        }

        //This implementation is the same as AutoUniqueLength, but takes duplicates into consideration

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        StringProperties props = config.DataProperties.StringProps!.Value;
        uint minLen = props.LengthData.Min;
        uint maxLen = props.LengthData.Max;

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        bool uniq = true;
        foreach (string value in stringArr)
        {
            ref List<string>? item = ref lengths[value.Length];
            item ??= new List<string>();
            item.Add(value);

            if (item.Count > 1)
                uniq = false;
        }

        context = new KeyLengthContext(lengths, uniq, minLen, maxLen);
        return true;
    }
}