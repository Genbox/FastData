using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal.Extensions;
using Genbox.FastData.Models;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class UniqueKeyLengthSwitchCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, UniqueKeyLengthSwitchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetMethodAttributes()}}
              public{{cfg.GetModifier()}} bool Contains({{genCfg.DataType}} value)
              {
          {{cfg.GetEarlyExits(genCfg, "value")}}

                  switch (value.Length)
                  {
          {{GenerateSwitch(genCfg, ctx.Data)}}
                      default:
                          return false;
                  }
              }
          """;

    private static string GenerateSwitch(GeneratorConfig genCfg, object[] values)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string value in values)
        {
            sb.AppendLine($"""
                                       case {value.Length}:
                                           return {genCfg.GetEqualFunction("value", ToValueLabel(value))};
                           """);
        }

        return sb.ToString();
    }
}