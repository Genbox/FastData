namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class EytzingerSearchCode(GeneratorConfig genCfg, CSharpCodeGeneratorConfig cfg, EytzingerSearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}{{genCfg.GetTypeName()}}[] _entries = new {{genCfg.GetTypeName()}}[] {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              {{cfg.GetMethodAttributes()}}
              {{cfg.GetMethodModifier()}}bool Contains({{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  int i = 0;
                  while (i < _entries.Length)
                  {
                      int comparison = {{genCfg.GetCompareFunction("_entries[i]")}};

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
}