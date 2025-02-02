using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthCode(FastDataSpec Spec, GeneratorContext Context) : ICode
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
        string?[] lengths = new string?[Spec.Data.Length + 1];

        int lowerBound = int.MaxValue;

        foreach (string? value in Spec.Data)
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

    public string Generate()
    {
        return $$"""
                     private{{GetModifier(Spec.ClassType)}} readonly {{Spec.DataTypeName}}[] _entries = new {{Spec.DataTypeName}}[] {
                 {{JoinValues(_lengths.AsSpan(_lowerBound), Render, ",\n")}}
                     };

                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", Context.GetEarlyExits(), true)}}

                         return {{GetEqualFunction("value", $"_entries[value.Length - {_lowerBound.ToString(NumberFormatInfo.InvariantInfo)}]")}};
                     }
                 """;

        static void Render(StringBuilder sb, string? obj)
        {
            if (obj == null)
                sb.Append("        null");
            else
                sb.Append("        ").Append(ToValueLabel(obj));
        }
    }
}