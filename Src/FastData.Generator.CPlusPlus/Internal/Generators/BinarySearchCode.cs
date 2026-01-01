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
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;
        bool useInterpolation = ctx.UseInterpolation;

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

                    """);

        if (useInterpolation)
        {
            sb.Append($$"""
                                int32_t lo = 0;
                                int32_t hi = {{(keys.Length - 1).ToStringInvariant()}};
                                while (lo <= hi && {{LookupKeyName}} >= keys[lo] && {{LookupKeyName}} <= keys[hi]) {
                                    const {{KeyTypeName}} lo_key = keys[lo];
                                    const {{KeyTypeName}} hi_key = keys[hi];

                                    if (lo_key == hi_key) {
                                        if (lo_key == {{LookupKeyName}})
                                            return true;

                                        break;
                                    }

                                    const double range = static_cast<double>(hi_key) - static_cast<double>(lo_key);
                                    const double offset = static_cast<double>({{LookupKeyName}}) - static_cast<double>(lo_key);
                                    int32_t mid = lo + static_cast<int32_t>((offset * (hi - lo)) / range);

                                    if (mid < lo)
                                        mid = lo;
                                    else if (mid > hi)
                                        mid = hi;

                                    const {{KeyTypeName}} mid_key = keys[mid];
                                    if (mid_key == {{LookupKeyName}})
                                        return true;
                                    if (mid_key < {{LookupKeyName}})
                                        lo = mid + 1;
                                    else
                                        hi = mid - 1;
                                }

                                return false;
                            }
                        """);
        }
        else
        {
            sb.Append($$"""
                                int32_t lo = 0;
                                int32_t hi = {{(keys.Length - 1).ToStringInvariant()}};
                                while (lo <= hi) {
                                    const int32_t mid = lo + ((hi - lo) >> 1);
                                    const {{KeyTypeName}} mid_key = keys[mid];
                                    const int32_t order = {{GetCompareFunction("mid_key", LookupKeyName)}};

                                    if (order == 0)
                                        return true;
                                    if (order < 0)
                                        lo = mid + 1;
                                    else
                                        hi = mid - 1;
                                }

                                return false;
                            }
                        """);
        }

        if (!ctx.Values.IsEmpty)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} {{InputKeyName}}, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                        """);

            if (useInterpolation)
            {
                sb.Append($$"""
                                    int32_t lo = 0;
                                    int32_t hi = {{(keys.Length - 1).ToStringInvariant()}};
                                    while (lo <= hi && {{LookupKeyName}} >= keys[lo] && {{LookupKeyName}} <= keys[hi]) {
                                        const {{KeyTypeName}} lo_key = keys[lo];
                                        const {{KeyTypeName}} hi_key = keys[hi];

                                        if (lo_key == hi_key) {
                                            if (lo_key == {{LookupKeyName}})
                                            {
                                                value = {{ptr}}values[lo];
                                                return true;
                                            }

                                            break;
                                        }

                                        const double range = static_cast<double>(hi_key) - static_cast<double>(lo_key);
                                        const double offset = static_cast<double>({{LookupKeyName}}) - static_cast<double>(lo_key);
                                        int32_t mid = lo + static_cast<int32_t>((offset * (hi - lo)) / range);

                                        if (mid < lo)
                                            mid = lo;
                                        else if (mid > hi)
                                            mid = hi;

                                        const {{KeyTypeName}} mid_key = keys[mid];
                                        if (mid_key == {{LookupKeyName}})
                                        {
                                            value = {{ptr}}values[mid];
                                            return true;
                                        }

                                        if (mid_key < {{LookupKeyName}})
                                            lo = mid + 1;
                                        else
                                            hi = mid - 1;
                                    }

                                    value = nullptr;
                                    return false;
                                }
                            """);
            }
            else
            {
                sb.Append($$"""
                                    int32_t lo = 0;
                                    int32_t hi = {{(keys.Length - 1).ToStringInvariant()}};
                                    while (lo <= hi) {
                                        const int32_t mid = lo + ((hi - lo) >> 1);
                                        const {{KeyTypeName}} mid_key = keys[mid];
                                        const int32_t order = {{GetCompareFunction("mid_key", LookupKeyName)}};

                                        if (order == 0)
                                        {
                                            value = {{ptr}}values[mid];
                                            return true;
                                        }

                                        if (order < 0)
                                            lo = mid + 1;
                                        else
                                            hi = mid - 1;
                                    }

                                    value = nullptr;
                                    return false;
                                }
                            """);
            }
        }

        return sb.ToString();
    }
}