using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ArrayCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate() => true;

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

                         for (int i = 0; i < {{Spec.Data.Length.ToString(NumberFormatInfo.InvariantInfo)}}; i++)
                         {
                             if ({{GetEqualFunction("value", "_entries[i]")}})
                                return true;
                         }
                         return false;
                     }
                 """;

        static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
    }
}