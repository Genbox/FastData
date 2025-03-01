using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ConditionalCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate() => true;

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  if ({{JoinValues(config.Data, Render, " || ")}})
                      return true;

                  return false;
              }
          """;

    private void Render(StringBuilder sb, object obj) => sb.Append(GetEqualFunction(config.DataType, "value", ToValueLabel(obj)));
}