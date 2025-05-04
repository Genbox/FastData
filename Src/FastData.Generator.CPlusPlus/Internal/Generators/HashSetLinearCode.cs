using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetLinearCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, HashSetLinearContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              struct b
              {
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} start_index;
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} end_index;

                  b(const {{GetSmallestUnsignedType(ctx.Data.Length)}} start_index, const {{GetSmallestUnsignedType(ctx.Data.Length)}} end_index)
                  : start_index(start_index), end_index(end_index) { }
              };

              {{cfg.GetFieldModifier(false)}}std::array<b, {{ctx.Buckets.Length}}> buckets = {
          {{FormatColumns(ctx.Buckets, static x => $"b({x.StartIndex.ToStringInvariant()}, {x.EndIndex.ToStringInvariant()})")}}
              };

              {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName()}}, {{ctx.Data.Length}}> items = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{cfg.GetFieldModifier()}}std::array<uint32_t, {{ctx.HashCodes.Length}}> hash_codes = {
          {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
              };

          {{genCfg.GetHashSource()}}

          public:
              [[nodiscard]]
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  const uint32_t hash = get_hash(value);
                  const auto& [start_index, end_index]= buckets[{{cfg.GetModFunction(ctx.Buckets.Length)}}];

                  {{GetSmallestUnsignedType(ctx.Data.Length)}} index = start_index;

                  while (index <= end_index)
                  {
                      if (hash_codes[index] == hash && items[index] == value)
                          return true;

                      index++;
                  }

                  return false;
              }
          """;
}