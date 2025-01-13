using System.Globalization;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchHashCode(FastDataSpec Spec) : ICode
{
    private (uint, object)[] _hashCodes;

    public bool IsAppropriate(DataProperties dataProps) => true;

    public bool TryPrepare()
    {
        (uint, object)[] hashCodes = new (uint, object)[Spec.Data.Length];

        for (int i = 0; i < Spec.Data.Length; i++)
        {
            object value = Spec.Data[i];
            uint hash = HashHelper.HashObject(value);

            hashCodes[i] = (hash, value);
        }

        _hashCodes = hashCodes;
        return true;
    }

    public string Generate(IEnumerable<IEarlyExit> ee)
    {
        return $$"""
                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", ee)}}

                         switch ({{GetHashFunction32(Spec.KnownDataType, "value")}})
                         {
                 {{JoinValues(_hashCodes, Render, "\n")}}
                         }
                         return false;
                     }
                 """;

        void Render(StringBuilder sb, (uint, object) obj)
        {
            sb.Append($"""
                                   case {obj.Item1.ToString(NumberFormatInfo.InvariantInfo)}:
                                        return {GetEqualFunction("value", ToValueLabel(obj.Item2))};
                       """);
        }
    }
}