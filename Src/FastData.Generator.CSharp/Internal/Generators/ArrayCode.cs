namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode(GeneratorConfig genCfg, CSharpGeneratorConfig cfg, ArrayContext ctx) : IOutputWriter
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

                  for (int i = 0; i < {{ctx.Data.Length.ToString(NumberFormatInfo.InvariantInfo)}}; i++)
                  {
                      if ({{genCfg.GetEqualFunction("_entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}