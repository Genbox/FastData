using System.Globalization;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchHashCode(FastDataConfig config, GeneratorContext context) : ICode
{
    private (uint, object)[] _hashCodes;

    public bool TryCreate()
    {
        (uint, object)[] hashCodes = new (uint, object)[config.Data.Length];

        for (int i = 0; i < config.Data.Length; i++)
        {
            object value = config.Data[i];
            uint hash = HashHelper.HashObject(value);

            hashCodes[i] = (hash, value);
        }

        _hashCodes = hashCodes;
        return true;
    }

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  switch ({{GetHashFunction32(config.DataType, "value")}})
                  {
          {{JoinValues(_hashCodes, Render, "\n")}}
                  }
                  return false;
              }
          """;

    private void Render(StringBuilder sb, (uint, object) obj)
    {
        sb.Append($"""
                               case {obj.Item1.ToString(NumberFormatInfo.InvariantInfo)}:
                                    return {GetEqualFunction(config.DataType, "value", ToValueLabel(obj.Item2))};
                   """);
    }
}