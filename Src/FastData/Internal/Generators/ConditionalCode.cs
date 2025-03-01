using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ConditionalCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate() => true;

    public string Generate() =>
        $$"""
              {{GetMethodAttributes()}}
              public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
              {
          {{GetEarlyExits("value", Context.GetEarlyExits())}}

                  if ({{JoinValues(Spec.Data, Render, " || ")}})
                      return true;

                  return false;
              }
          """;

    private void Render(StringBuilder sb, object obj) => sb.Append(GetEqualFunction(Spec.KnownDataType, "value", ToValueLabel(obj)));
}