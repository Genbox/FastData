using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate() => true;

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  switch (value)
                  {
          {{JoinValues(config.Data, Render, "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append($"            case {ToValueLabel(obj)}:");
}