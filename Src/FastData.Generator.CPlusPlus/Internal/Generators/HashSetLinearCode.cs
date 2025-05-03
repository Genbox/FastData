using Genbox.FastData.Contexts.Misc;

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

              {{cfg.GetFieldModifier()}}std::array<b, {{ctx.Buckets.Length}}> buckets = {
          {{FormatColumns(ctx.Buckets, RenderBucket)}}
              };

              {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName(false)}}, {{ctx.Data.Length}}> items = {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

              {{cfg.GetFieldModifier()}}std::array<uint32_t, {{ctx.HashCodes.Length}}> hash_codes = {
          {{FormatColumns(ctx.HashCodes, static (sb, obj) => sb.Append(obj))}}
              };

          {{genCfg.GetHashSource()}}

          public:
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value)
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

    private static void RenderBucket(StringBuilder sb, HashSetBucket x) => sb.Append("b(").Append(x.StartIndex).Append(", ").Append(x.EndIndex).Append(')');
}