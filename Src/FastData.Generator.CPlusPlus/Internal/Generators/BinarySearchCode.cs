using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode<T>(BinarySearchContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) =>
        $$"""
              {{FieldModifier}}std::array<{{TypeName}}, {{data.Length.ToStringInvariant()}}> entries = {
          {{FormatColumns(data, ToValueLabel)}}
              };

          public:
              {{MethodAttribute}}
              {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
              {
          {{EarlyExits}}

                  {{ArraySizeType}} lo = 0;
                  {{ArraySizeType}} hi = {{(data.Length - 1).ToStringInvariant()}};
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