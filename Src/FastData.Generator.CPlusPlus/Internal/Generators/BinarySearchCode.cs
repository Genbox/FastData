using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode<T>(BinarySearchContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() =>
        $$"""
              {{GetFieldModifier()}}std::array<{{TypeName}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

          public:
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
              {
          {{GetEarlyExits()}}

                  {{GetArraySizeType()}} lo = 0;
                  {{GetArraySizeType()}} hi = {{(ctx.Data.Length - 1).ToStringInvariant()}};
                  while (lo <= hi)
                  {
                      const size_t mid = lo + ((hi - lo) >> 1);

                      if ({{GetEqualFunction("entries[mid]", "value")}})
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