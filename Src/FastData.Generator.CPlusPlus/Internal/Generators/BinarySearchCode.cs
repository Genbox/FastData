using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            sb.Append($$"""
                            {{GetFieldModifier(false)}}std::array<{{GetValueTypeName(customValue)}}, {{ctx.Values.Length.ToStringInvariant()}}> values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{GetFieldModifier(true)}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        };

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetEarlyExits(MethodType.Contains)}}

                            int32_t lo = 0;
                            int32_t hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                            while (lo <= hi) {
                                const int32_t mid = lo + ((hi - lo) >> 1);
                                const {{KeyTypeName}} mid_key = keys[mid];

                                if ({{GetEqualFunction("mid_key", "key")}})
                                    return true;

                                if (mid_key < key)
                                    lo = mid + 1;
                                else
                                    hi = mid - 1;
                            }

                            return false;
                        }
                    """);

        if (ctx.Values != null)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetEarlyExits(MethodType.TryLookup)}}

                                int32_t lo = 0;
                                int32_t hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                                while (lo <= hi) {
                                    const int32_t mid = lo + ((hi - lo) >> 1);
                                    const {{KeyTypeName}} mid_key = keys[mid];

                                    if ({{GetEqualFunction("mid_key", "key")}})
                                    {
                                        value = {{ptr}}values[mid];
                                        return true;
                                    }

                                    if (mid_key < key)
                                        lo = mid + 1;
                                    else
                                        hi = mid - 1;
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}