using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{FieldModifier}}{{KeyTypeName}}[] _keys = new {{KeyTypeName}}[] {
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        };

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            int lo = 0;
                            int hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                            while (lo <= hi)
                            {
                                int i = lo + ((hi - lo) >> 1);
                                int order = {{GetCompareFunction("_keys[i]", LookupKeyName)}};

                                if (order == 0)
                                    return true;
                                if (order < 0)
                                    lo = i + 1;
                                else
                                    hi = i - 1;
                            }

                            return ~lo >= 0;
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}}? value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                int lo = 0;
                                int hi = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                                while (lo <= hi)
                                {
                                    int i = lo + ((hi - lo) >> 1);
                                    int order = {{GetCompareFunction("_keys[i]", LookupKeyName)}};

                                    if (order == 0)
                                    {
                                        value = _values[i];
                                        return true;
                                    }
                                    if (order < 0)
                                        lo = i + 1;
                                    else
                                        hi = i - 1;
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}