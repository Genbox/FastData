using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class KeyLengthStructure<T>(StringProperties props) : IStructure<T, KeyLengthContext<T>>
{
    public KeyLengthContext<T> Create(ReadOnlySpan<T> data)
    {
        if (typeof(T) != typeof(string))
            throw new InvalidCastException("This structure only works on strings");

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        uint maxLen = props.LengthData.Max;

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        foreach (T value in data)
        {
            string str = (string)(object)value;

            ref List<string>? item = ref lengths[str.Length];
            item ??= new List<string>();
            item.Add(str);
        }

        return new KeyLengthContext<T>(lengths, props.LengthData.Unique, props.LengthData.Min, maxLen);
    }
}