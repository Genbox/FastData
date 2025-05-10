using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode<T>(GeneratorConfig<T> genCfg, CSharpCodeGeneratorConfig cfg, ArrayContext<T> ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}{{genCfg.GetTypeName()}}[] _entries = new {{genCfg.GetTypeName()}}[] {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  for (int i = 0; i < {{ctx.Data.Length.ToStringInvariant()}}; i++)
                  {
                      if ({{genCfg.GetEqualFunction("_entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}