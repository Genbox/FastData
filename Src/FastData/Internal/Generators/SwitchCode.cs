using System.Text;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchCode(FastDataSpec Spec) : ICode
{
    public bool IsAppropriate(DataProperties dataProps) => true;

    public bool TryPrepare() => true;

    public string Generate(IEnumerable<IEarlyExit> ee)
    {
        return $$"""
                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", ee)}}

                         switch (value)
                         {
                 {{JoinValues(Spec.Data, Render, "\n")}}
                                 return true;
                             default:
                                 return false;
                         }
                     }
                 """;

        static void Render(StringBuilder sb, object obj) => sb.Append($"            case {ToValueLabel(obj)}:");
    }
}