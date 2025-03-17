using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Models;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthCode : IStructure
{
    //TODO: Remove gaps in array by reducing the index via a map (if (idx > 10) return 4) where 4 is the number to subtract from the index

    public IContext Create(object[] data)
    {
        //The idea here is to fit the strings into an array indexed on length. For example:
        //idx 0: ""
        //idx 1: "a"
        //idx 2: null
        //idx 3: "aaa"

        //It is efficient since we don't need a hash function to look up the element, but if there is a big gap in the lengths,
        //we will store a lot of empty elements.
        string?[] lengths = new string?[data.Length + 1];

        int lowerBound = int.MaxValue;

        foreach (string? value in data)
        {
            ref string? item = ref lengths[value.Length];

            //Ensure this generator only works on values that all have unique length
            if (item != null)
                throw new InvalidOperationException("Duplicate length detected");

            lowerBound = Math.Min(lowerBound, value.Length);
            item = value;
        }

        return new UniqueKeyLengthContext(data, lengths, lowerBound);
    }
}