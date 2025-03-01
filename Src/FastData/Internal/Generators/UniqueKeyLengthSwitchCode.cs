using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class UniqueKeyLengthSwitchCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate() => true;

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  switch (value.Length)
                  {
          {{GenerateSwitch(config.Data)}}
                      default:
                          return false;
                  }
              }
          """;

    private string GenerateSwitch(object[] values)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string value in values)
        {
            sb.AppendLine($"""
                                       case {value.Length}:
                                           return {GetEqualFunction(config.DataType, "value", ToValueLabel(value))};
                           """);
        }

        return sb.ToString();
    }
}