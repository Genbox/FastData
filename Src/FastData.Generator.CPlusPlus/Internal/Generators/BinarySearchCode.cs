namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, BinarySearchContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName()}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

          public:
              [[nodiscard]]
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  size_t lo = 0;
                  size_t hi = {{(ctx.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                  while (lo <= hi)
                  {
                      const size_t mid = lo + ((hi - lo) >> 1);

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