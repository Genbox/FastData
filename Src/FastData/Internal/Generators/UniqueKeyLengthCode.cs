using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class UniqueKeyLengthCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        //The idea here is to fit the strings into an array indexed on length. For example:
        //idx 0: ""
        //idx 1: "a"
        //idx 2: null
        //idx 3: "aaa"

        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        //Sanity check on inputs
        HashSet<int> uniqLen = new HashSet<int>();

        foreach (string value in spec.Data)
        {
            if (!uniqLen.Add(value.Length))
                throw new InvalidOperationException("Not able to generate a unique length index as the data does not have unique lengths");
        }

        //It is efficient since we don't need a hash function to lookup the element, but if there is a big gap in the lengths,
        //we will store a lot of empty elements.
        string?[] lengths = new string?[spec.Data.Length + 1];

        int lowerBound = int.MaxValue;

        foreach (string? value in spec.Data)
        {
            ref string? item = ref lengths[value.Length];

            //Ensure this generator only works on values that all have unique length
            if (item != null)
                throw new InvalidOperationException("Duplicate length detected");

            lowerBound = Math.Min(lowerBound, value.Length);
            item = value;
        }

        //TODO: Remove gaps in array by reducing the index via a map (if (idx > 10) return 4) where 4 is the number to subtract from the index

        sb.Append($$"""
                        private{{staticStr}} readonly {{spec.DataTypeName}}[] _entries = new {{spec.DataTypeName}}[] {
                    {{GenerateList(lengths, lowerBound)}}
                        };

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs, true)}}

                            return {{GetEqualFunction("value", $"_entries[value.Length - {lowerBound.ToString(NumberFormatInfo.InvariantInfo)}]")}};
                        }
                    """);
    }

    private static string GenerateList(string?[] data, int lowerBound)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = lowerBound; i < data.Length; i++)
        {
            string? value = data[i];

            if (value == null)
                sb.Append("        null");
            else
                sb.Append("        ").Append(ToValueLabel(value));

            if (i != data.Length - 1)
                sb.AppendLine(", ");
        }

        return sb.ToString();
    }
}