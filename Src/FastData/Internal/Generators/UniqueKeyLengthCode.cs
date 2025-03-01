using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthCode(FastDataConfig config, GeneratorContext context) : ICode
{
    private string?[] _lengths;
    private int _lowerBound;

    public bool TryCreate()
    {
        //The idea here is to fit the strings into an array indexed on length. For example:
        //idx 0: ""
        //idx 1: "a"
        //idx 2: null
        //idx 3: "aaa"

        //It is efficient since we don't need a hash function to lookup the element, but if there is a big gap in the lengths,
        //we will store a lot of empty elements.
        string?[] lengths = new string?[config.Data.Length + 1];

        int lowerBound = int.MaxValue;

        foreach (string? value in config.Data)
        {
            ref string? item = ref lengths[value.Length];

            //Ensure this generator only works on values that all have unique length
            if (item != null)
                throw new InvalidOperationException("Duplicate length detected");

            lowerBound = Math.Min(lowerBound, value.Length);
            item = value;
        }

        _lengths = lengths;
        _lowerBound = lowerBound;
        return true;
    }

    //TODO: Remove gaps in array by reducing the index via a map (if (idx > 10) return 4) where 4 is the number to subtract from the index

    public string Generate() =>
        $$"""
              private{{GetModifier(config.ClassType)}} readonly {{config.DataType}}[] _entries = new {{config.DataType}}[] {
          {{JoinValues(_lengths.AsSpan(_lowerBound), Render, ",\n")}}
              };

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits(), true)}}

                  return {{GetEqualFunction(config.DataType, "value", $"_entries[value.Length - {_lowerBound.ToString(NumberFormatInfo.InvariantInfo)}]")}};
              }
          """;

    private static void Render(StringBuilder sb, string? obj)
    {
        if (obj == null)
            sb.Append("        null");
        else
            sb.Append("        ").Append(ToValueLabel(obj));
    }
}