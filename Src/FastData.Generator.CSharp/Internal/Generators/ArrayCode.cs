using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}{{TypeName}}[] _entries = new {{TypeName}}[] {
          {{FormatColumns(data, ToValueLabel)}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{TypeName}} value)
              {
          {{EarlyExits}}

                  for (int i = 0; i < {{data.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{GetEqualFunction("value", "_entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}