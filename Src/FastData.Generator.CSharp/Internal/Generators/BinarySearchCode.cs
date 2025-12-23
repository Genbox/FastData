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
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{FieldModifier}}{{KeyTypeName}}[] _keys = new {{KeyTypeName}}[] {
                    {{FormatColumns(keys, ToValueLabel)}}
                        };

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            int lo = 0;
                            int hi = {{(keys.Length - 1).ToStringInvariant()}};
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

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}}? value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                int lo = 0;
                                int hi = {{(keys.Length - 1).ToStringInvariant()}};
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