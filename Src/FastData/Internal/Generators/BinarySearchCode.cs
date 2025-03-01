using System.Globalization;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class BinarySearchCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate()
    {
        Array.Sort(config.Data, StringComparer.Ordinal);
        return true;
    }

    public string Generate() =>
        $$"""
              private{{GetModifier(config.ClassType)}} {{config.DataType}}[] _entries = new {{config.DataType}}[] {
          {{JoinValues(config.Data, Render, ",\n")}}
              };

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  int lo = 0;
                  int hi = {{(config.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                  while (lo <= hi)
                  {
                      int i = lo + ((hi - lo) >> 1);
                      int order = {{GetCompareFunction("_entries[i]", "value")}};

                      if (order == 0)
                          return true;
                      if (order < 0)
                          lo = i + 1;
                      else
                          hi = i - 1;
                  }

                  return ((~lo) >= 0);
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
}