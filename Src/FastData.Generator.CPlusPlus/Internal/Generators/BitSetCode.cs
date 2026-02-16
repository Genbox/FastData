using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BitSetCode<TKey, TValue>(BitSetContext<TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{GetFieldModifier(true)}}std::array<uint64_t, {{ctx.BitSet.Length.ToStringInvariant()}}> bitset = {
                    {{FormatColumns(ctx.BitSet, ToValueLabel)}}
                        };

                    """);

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            sb.Append($$"""
                            {{GetFieldModifier(false)}}std::array<{{GetValueTypeName(customValue)}}, {{values.Length.ToStringInvariant()}}> values = {
                        {{FormatColumns(values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} {{InputKeyName}}){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}

                            const uint64_t offset = static_cast<uint64_t>({{LookupKeyName}} - min_key);
                            const size_t word = static_cast<size_t>(offset >> 6);
                            return (bitset[word] & (1ULL << (offset & 63))) != 0;
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} {{InputKeyName}}, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                const uint64_t offset = static_cast<uint64_t>({{LookupKeyName}} - min_key);
                                const size_t word = static_cast<size_t>(offset >> 6);
                                if ((bitset[word] & (1ULL << (offset & 63))) == 0)
                                {
                                    value = nullptr;
                                    return false;
                                }

                                value = {{ptr}}values[static_cast<size_t>(offset)];
                                return true;
                            }
                        """);
        }

        return sb.ToString();
    }
}