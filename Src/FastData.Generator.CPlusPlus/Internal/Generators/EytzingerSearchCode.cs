namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class EytzingerSearchCode<T>(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, EytzingerSearchContext<T> ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName()}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

          public:
              [[nodiscard]]
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  size_t i = 0;
                  while (i < entries.size())
                  {
                      if (entries[i] == value)
                          return true;

                      if (entries[i] < value)
                          i = 2 * i + 2;
                      else
                          i = 2 * i + 1;
                  }

                  return false;
              }
          """;
}