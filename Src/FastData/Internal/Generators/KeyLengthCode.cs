using System.Text;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Enums;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class KeyLengthCode(FastDataSpec Spec) : ICode
{
    private List<string>?[] _lengths;

    public bool IsAppropriate(DataProperties dataProps) => Spec.KnownDataType == KnownDataType.String;

    public bool TryPrepare()
    {
        //This implementation is the same as AutoUniqueLength, but takes duplicates into consideration

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        //Calculate the maximum length
        int maxLen = Spec.Data.Cast<string>().Max(x => x.Length);

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        foreach (string value in Spec.Data)
        {
            ref List<string>? item = ref lengths[value.Length];
            item ??= new List<string>();
            item.Add(value);
        }

        _lengths = lengths;
        return true;
    }

    public string Generate(IEnumerable<IEarlyExit> ee)
    {
        return $$"""
                     private{{GetModifier(Spec.ClassType)}} readonly {{Spec.DataTypeName}}[]?[] _entries = [
                 {{JoinValues(_lengths, RenderEntry, ",\n")}}
                     ];

                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", ee, true)}}

                         {{Spec.DataTypeName}}[]? bucket = _entries[value.Length];

                         if (bucket == null)
                             return false;

                         foreach ({{Spec.DataTypeName}} str in bucket)
                         {
                             if ({{GetEqualFunction("value", "str")}})
                                 return true;
                         }

                         return false;
                     }
                 """;

        static void RenderEntry(StringBuilder sb, List<string>? obj)
        {
            if (obj == null)
                sb.Append("        null");
            else
                sb.Append("        [").Append(string.Join(",", obj.Select(x => "\"" + x + "\""))).Append(']');
        }
    }
}