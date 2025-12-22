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
                            {{GetFieldModifier(true)}}std::array<int32_t, {{ctx.ValueOffsets.Length}}> offsets = {
                        {{FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}}
                            };

                            {{GetFieldModifier(false)}}std::array<{{GetValueTypeName(customValue)}}, {{ctx.Values.Length}}> values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{GetFieldModifier(true)}}std::array<{{KeyTypeName}}, {{ctx.Lengths.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                        };

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}

                            return {{GetEqualFunction(LookupKeyName, $"keys[{LookupKeyName}.length() - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (ctx.Values != null)
        {
            string ptr = customValue ? "" : "&";
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                size_t idx = {{LookupKeyName}}.length() - {{ctx.MinLength.ToStringInvariant()}};
                                if ({{GetEqualFunction(LookupKeyName, "keys[idx]")}}) {
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