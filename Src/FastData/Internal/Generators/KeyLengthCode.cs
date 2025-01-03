using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class KeyLengthCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        //This implementation is the same as AutoUniqueLength, but takes duplicates into consideration

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        //Calculate the maximum length
        int maxLen = spec.Data.Cast<string>().Max(x => x.Length);

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        foreach (string value in spec.Data)
        {
            ref List<string>? item = ref lengths[value.Length];
            item ??= new List<string>();
            item.Add(value);
        }

        sb.Append($$"""
                        private{{staticStr}} readonly {{spec.DataTypeName}}[]?[] _entries = [
                    {{GenerateList(lengths)}}
                        ];

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs, true)}}

                            {{spec.DataTypeName}}[]? bucket = _entries[value.Length];

                            if (bucket == null)
                                return false;

                            foreach ({{spec.DataTypeName}} str in bucket)
                            {
                                if ({{GetEqualFunction("value", "str")}})
                                    return true;
                            }

                            return false;
                        }
                    """);
    }

    private static string GenerateList(List<string>?[] data)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            List<string>? values = data[i];

            if (values == null)
                sb.Append("        null");
            else
                sb.Append("        [").Append(string.Join(",", values.Select(x => "\"" + x + "\""))).Append(']');

            if (i != data.Length - 1)
                sb.AppendLine(", ");
        }

        return sb.ToString();
    }
}