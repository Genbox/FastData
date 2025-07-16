using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx, SharedCode shared, string className) : CPlusPlusOutputWriter<TKey, TValue>(ctx.Values)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($$"""
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

        if (ctx.Values != null && ObjectType != null)
        {
            shared.Add("values", CodePlacement.After, $$"""
                                                        std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> {{className}}::values = {
                                                        {{ValueString}}
                                                        };
                                                        """);

            if (ObjectType.IsCustomType)
                shared.Add("classes", CodePlacement.Before, GetObjectDeclarations(ObjectType));

            sb.Append($$"""
                            static std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> values;

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
                                        value = {{(ObjectType.IsCustomType ? "" : "&")}}values[mid];
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