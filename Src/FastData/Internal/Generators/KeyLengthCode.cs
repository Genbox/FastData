using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class KeyLengthCode(FastDataConfig config, GeneratorContext context) : ICode
{
    private List<string>?[] _lengths;

    public bool TryCreate()
    {
        //This implementation is the same as AutoUniqueLength, but takes duplicates into consideration

        //idx 0: ""
        //idx 1: "a", "b"
        //idx 2: null
        //idx 3: "aaa", "bbb"

        //Calculate the maximum length
        int maxLen = config.Data.Cast<string>().Max(x => x.Length);

        //We don't have to use HashSets to deduplicate within a bucket as all items are unique
        List<string>?[] lengths = new List<string>?[maxLen + 1]; //We need a place for zero

        foreach (string value in config.Data)
        {
            ref List<string>? item = ref lengths[value.Length];
            item ??= new List<string>();
            item.Add(value);
        }

        _lengths = lengths;
        return true;
    }

    public string Generate() =>
        $$"""
              private{{GetModifier(config.ClassType)}} readonly {{config.DataType}}[]?[] _entries = [
          {{JoinValues(_lengths, RenderEntry, ",\n")}}
              ];

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits(), true)}}

                  {{config.DataType}}[]? bucket = _entries[value.Length];

                  if (bucket == null)
                      return false;

                  foreach ({{config.DataType}} str in bucket)
                  {
                      if ({{GetEqualFunction(config.DataType, "value", "str")}})
                          return true;
                  }

                  return false;
              }
          """;

    private static void RenderEntry(StringBuilder sb, List<string>? obj)
    {
        if (obj == null)
            sb.Append("        null");
        else
            sb.Append("        [").Append(string.Join(",", obj.Select(x => "\"" + x + "\""))).Append(']');
    }
}