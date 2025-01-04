using System.Text;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Enums;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthSwitchCode(FastDataSpec Spec) : ICode
{
    public bool IsAppropriate(DataProperties dataProps) => Spec.KnownDataType == KnownDataType.String && dataProps.StringProps.NumLengths == Spec.Data.Length;

    public bool TryPrepare() => true;

    public string Generate(IEnumerable<IEarlyExit> earlyExits)
        => $$"""
                 {{GetMethodAttributes()}}
                 public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                 {
             {{GetEarlyExits("value", earlyExits)}}

                     switch (value.Length)
                     {
             {{GenerateSwitch(Spec.Data)}}
                         default:
                             return false;
                     }
                 }
             """;

    private static string GenerateSwitch(object[] values)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string value in values)
        {
            sb.AppendLine($"""
                                       case {value.Length}:
                                           return {GetEqualFunction("value", ToValueLabel(value))};
                           """);
        }

        return sb.ToString();
    }
}