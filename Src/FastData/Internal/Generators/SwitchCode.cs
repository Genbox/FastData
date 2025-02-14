using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SwitchCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate() => true;

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
              {
          {{GetEarlyExits("value", Context.GetEarlyExits())}}

                  switch (value)
                  {
          {{JoinValues(Spec.Data, Render, "\n")}}
                          return true;
                      default:
                          return false;
                  }
              }
          """;

    private static void Render(StringBuilder sb, object obj) => sb.Append($"            case {ToValueLabel(obj)}:");
}