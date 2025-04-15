using Genbox.FastData.Abstracts;
using Genbox.FastData.Contexts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Structures;

internal sealed class KeyLengthStructure(StructureConfig config) : IStructure
{
    public bool TryCreate(object[] data, out IContext? context)
    {
        // This data structure is only appropriate for strings
        if (config.DataProperties.DataType != DataType.String)
        {
            context = null;
            return false;
        }

        //This implementation is the same as AutoUniqueLength, but takes duplicates into consideration

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        uint minLen = config.DataProperties.StringProps!.Value.LengthData.Min;
        uint maxLen = config.DataProperties.StringProps!.Value.LengthData.Max;

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        bool uniq = true;
        foreach (string value in data)
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