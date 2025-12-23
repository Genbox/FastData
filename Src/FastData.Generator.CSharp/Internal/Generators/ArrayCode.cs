using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
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

                            for (int i = 0; i < {{keys.Length.ToStringInvariant()}}; i++)
                            {
                                if ({{GetEqualFunction(LookupKeyName, "_keys[i]")}})
                                   return true;
                            }
                            return false;
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}}? value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                for ({{ArraySizeType}} i = 0; i < {{keys.Length.ToStringInvariant()}}; i++)
                                {
                                    if ({{GetEqualFunction("_keys[i]", LookupKeyName)}})
                                    {
                                        value = _values[i];
                                        return true;
                                    }
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}