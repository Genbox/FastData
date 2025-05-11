using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetLinearCode<T>(HashSetLinearContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              struct b
              {
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} start_index;
                  {{GetSmallestUnsignedType(ctx.Data.Length)}} end_index;

                  b(const {{GetSmallestUnsignedType(ctx.Data.Length)}} start_index, const {{GetSmallestUnsignedType(ctx.Data.Length)}} end_index)
                  : start_index(start_index), end_index(end_index) { }
              };

              {{GetFieldModifier(false)}}std::array<b, {{ctx.Buckets.Length}}> buckets = {
          {{FormatColumns(ctx.Buckets, static x => $"b({x.StartIndex.ToStringInvariant()}, {x.EndIndex.ToStringInvariant()})")}}
              };

              {{GetFieldModifier()}}std::array<{{TypeName}}, {{ctx.Data.Length}}> items = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

              {{GetFieldModifier()}}std::array<uint32_t, {{ctx.HashCodes.Length}}> hash_codes = {
          {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
              };

          {{GetHashSource()}}

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
              {
          {{GetEarlyExits()}}

                  const uint32_t hash = get_hash(value);
                  const auto& [start_index, end_index]= buckets[{{GetModFunction("hash", (ulong)ctx.Buckets.Length)}}];

                  {{GetSmallestUnsignedType(ctx.Data.Length)}} index = start_index;

                  while (index <= end_index)
                  {
                      if ({{GetEqualFunction("hash_codes[index]", "hash")}} && {{GetEqualFunction("items[index]", "value")}})
                          return true;

                      index++;
                  }

                  return false;
              }
          """;
}