using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthSwitchCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate() => true;

    public string Generate()
        => $$"""
                 {{GetMethodAttributes()}}
                 public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                 {
             {{GetEarlyExits("value", Context.GetEarlyExits())}}

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