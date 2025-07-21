using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customType = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        };

                    public:
                        {{MethodAttribute}}
                        {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
                        {
                    {{EarlyExits}}

                            {{ArraySizeType}} lo = 0;
                            {{ArraySizeType}} hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                            while (lo <= hi)
                            {
                                const size_t mid = lo + ((hi - lo) >> 1);

                                if ({{GetEqualFunction("keys[mid]", "key")}})
                                    return true;

                                if (keys[mid] < key)
                                    lo = mid + 1;
                                else
                                    hi = mid - 1;
                            }

                            return false;
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add("classes", CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{GetFieldModifier(false)}}std::array<{{ValueTypeName}}{{(customType ? "*" : "")}}, {{ctx.Values.Length.ToStringInvariant()}}> values = {
                            {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                                {{ArraySizeType}} lo = 0;
                                {{ArraySizeType}} hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                                while (lo <= hi)
                                {
                                    const size_t mid = lo + ((hi - lo) >> 1);

                                    if ({{GetEqualFunction("keys[mid]", "key")}})
                                    {
                                        value = {{(customType ? "" : "&")}}values[mid];
                                        return true;
                                    }

                                    if (keys[mid] < key)
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