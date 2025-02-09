using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class EytzingerSearchCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate()
    {
        Array.Sort(Spec.Data, StringComparer.Ordinal);

        object[] output = new object[Spec.Data.Length];
        int index = 0;
        EytzingerOrder(ref index);

        Spec.Data = output;
        return true;

        void EytzingerOrder(ref int arrIdx, int eytIdx = 0)
        {
            if (eytIdx < Spec.Data.Length)
            {
                EytzingerOrder(ref arrIdx, (2 * eytIdx) + 1);
                output[eytIdx] = Spec.Data[arrIdx++];
                EytzingerOrder(ref arrIdx, (2 * eytIdx) + 2);
            }
        }
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

                         int i = 0;
                         while (i < _entries.Length)
                         {
                             int comparison = {{GetCompareFunction("_entries[i]", "value")}};

                             if (comparison == 0)
                                 return true;

                             if (comparison < 0)
                                 i = 2 * i + 2;
                             else
                                 i = 2 * i + 1;
                         }

                         return false;
                     }
                 """;

        static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
    }
}