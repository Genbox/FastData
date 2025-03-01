using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ArrayCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate() => true;

    public string Generate() =>
        $$"""
              private{{GetModifier(config.ClassType)}} {{config.DataType}}[] _entries = new {{config.DataType}}[] {
          {{JoinValues(config.Data, Render, ",\n")}}
              };

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  for (int i = 0; i < {{config.Data.Length.ToString(NumberFormatInfo.InvariantInfo)}}; i++)
                  {
                      if ({{GetEqualFunction(config.DataType, "value", "_entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
}