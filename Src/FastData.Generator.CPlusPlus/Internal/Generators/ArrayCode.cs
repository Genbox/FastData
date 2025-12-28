using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;

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
                        {{GetFieldModifier(true)}}std::array<{{KeyTypeName}}, {{keys.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(keys, ToValueLabel)}}
                        };

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} {{InputKeyName}}){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}

                            for ({{ArraySizeType}} i = 0; i < {{keys.Length.ToStringInvariant()}}; i++)
                            {
                                if ({{GetEqualFunction("keys[i]", LookupKeyName)}})
                                    return true;
                            }
                            return false;
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

                                for ({{ArraySizeType}} i = 0; i < {{keys.Length.ToStringInvariant()}}; i++) {
                                    if ({{GetEqualFunction("keys[i]", LookupKeyName)}}) {
                                        value = {{ptr}}values[i];
                                        return true;
                                    }
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}