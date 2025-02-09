using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class BinarySearchCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate()
    {
        Array.Sort(Spec.Data, StringComparer.Ordinal);
        return true;
    }

    public string Generate()
    {
        return $$"""
                     private{{GetModifier(Spec.ClassType)}} {{Spec.DataTypeName}}[] _entries = new {{Spec.DataTypeName}}[] {
                 {{JoinValues(Spec.Data, Render, ",\n")}}
                     };

                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", Context.GetEarlyExits())}}

                         int lo = 0;
                         int hi = {{(Spec.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                         while (lo <= hi)
                         {
                             int i = lo + ((hi - lo) >> 1);
                             int order = {{GetCompareFunction("_entries[i]", "value")}};

                             if (order == 0)
                                 return true;
                             if (order < 0)
                                 lo = i + 1;
                             else
                                 hi = i - 1;
                         }

                         return ((~lo) >= 0);
                     }
                 """;

        static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
    }
}