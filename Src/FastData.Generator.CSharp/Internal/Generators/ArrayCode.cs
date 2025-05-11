using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode<T>(ArrayContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}{{TypeName}}[] _entries = new {{TypeName}}[] {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  for (int i = 0; i < {{ctx.Data.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{GetEqualFunction("value", "_entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}