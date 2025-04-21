namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, BinarySearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName(false)}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

          public:
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  int lo = 0;
                  int hi = {{(ctx.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                  while (lo <= hi)
                  {
                      const int mid = lo + ((hi - lo) >> 1);

                      if (entries[mid] == value)
                          return true;

                      if (entries[mid] < value)
                          lo = mid + 1;
                      else
                          hi = mid - 1;
                  }

                  return false;
              }
          """;
}