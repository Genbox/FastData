using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate(ReadOnlySpan<T> data) => ctx.StoreHashCode
        ? $$"""
            struct e
            {
                {{TypeName}} value;
                {{HashSizeType}} hash_code;

                e(const {{TypeName}} value, const {{HashSizeType}} hash_code)
                : value(value), hash_code(hash_code) {}
            };

            {{GetFieldModifier(false)}}std::array<e, {{ctx.Data.Length.ToStringInvariant()}}> entries = {
                {{FormatColumns(ctx.Data, x => $"e({ToValueLabel(x.Key)}, {x.Value.ToStringInvariant()})")}}
            };

            {{HashSource}}

            public:
                {{MethodAttribute}}
                {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
                {
            {{EarlyExits}}

                    const {{HashSizeType}} hash = get_hash(value);
                    const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                    const e& entry = entries[index];

                    return {{GetEqualFunction("hash", "entry.hash_code")}} && {{GetEqualFunction("value", "entry.value")}};
                }
            """
        : $$"""
            {{GetFieldModifier(false)}}std::array<{{TypeName}}, {{ctx.Data.Length.ToStringInvariant()}}> entries = {
                {{FormatColumns(ctx.Data, x => x.Value.ToStringInvariant())}}
            };

            {{HashSource}}

            public:
                {{MethodAttribute}}
                {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
                {
            {{EarlyExits}}

                    const {{HashSizeType}} hash = get_hash(value);
                    const {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};

                    return {{GetEqualFunction("value", "entries[index]")}};
                }
            """;
}