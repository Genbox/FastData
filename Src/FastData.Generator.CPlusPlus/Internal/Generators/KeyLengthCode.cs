using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}std::array<int32_t, {{ctx.ValueOffsets.Length}}> offsets = {
                        {{FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}}
                            };

                            {{GetFieldModifier(customValue)}}std::array<{{GetValueTypeName(customValue)}}, {{ctx.Values.Length}}> values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Lengths.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                        };

                    public:
                        {{MethodAttribute}}
                        {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetEarlyExits(MethodType.Contains)}}

                            return {{GetEqualFunction("key", $"keys[key.length() - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (ctx.Values != null)
        {
            string ptr = customValue ? "" : "&";
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, const {{GetValueTypeName(customValue)}}& value){{PostMethodModifier}} {
                        {{GetEarlyExits(MethodType.TryLookup)}}

                                size_t idx = key.length() - {{ctx.MinLength.ToStringInvariant()}};
                                if ({{GetEqualFunction("key", "keys[idx]")}}) {
                                    value = {{ptr}}values[offsets[idx]];
                                    return true;
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}