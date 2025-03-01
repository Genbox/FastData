using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class EytzingerSearchCode(FastDataConfig config, GeneratorContext context) : ICode
{
    public bool TryCreate()
    {
        Array.Sort(config.Data, StringComparer.Ordinal);

        object[] output = new object[config.Data.Length];
        int index = 0;
        EytzingerOrder(ref index);

        config.Data = output;
        return true;

        void EytzingerOrder(ref int arrIdx, int eytIdx = 0)
        {
            if (eytIdx < config.Data.Length)
            {
                EytzingerOrder(ref arrIdx, (2 * eytIdx) + 1);
                output[eytIdx] = config.Data[arrIdx++];
                EytzingerOrder(ref arrIdx, (2 * eytIdx) + 2);
            }
        }
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

                  int i = 0;
                  while (i < _entries.Length)
                  {
                      int comparison = {{GetCompareFunction("_entries[i]", "value")}};

                      if (comparison == 0)
                          return true;

                      if (comparison < 0)
                          i = 2 * i + 2;
                      else
                          i = 2 * i + 1;
                  }

                  return false;
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append("        ").Append(ToValueLabel(obj));
}