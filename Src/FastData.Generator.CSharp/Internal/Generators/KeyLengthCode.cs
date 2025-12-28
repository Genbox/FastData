using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}int[] _offsets = {
                        {{FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}}
                            };

                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{FieldModifier}}{{KeyTypeName}}[] _keys = {
                    {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                        };

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} {{InputKeyName}})
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            return {{GetEqualFunction(LookupKeyName, $"_keys[{LookupKeyName}.Length - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""
                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} {{InputKeyName}}, out {{ValueTypeName}} value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                int idx = {{LookupKeyName}}.Length - {{ctx.MinLength.ToStringInvariant()}};
                                if ({{GetEqualFunction(LookupKeyName, "_keys[idx]")}})
                                {
                                    value = _values[_offsets[idx]];
                                    return true;
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}