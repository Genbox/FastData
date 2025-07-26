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

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}int[] _offsets = {
                        {{FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}}
                            };

                        """);

            sb.Append($$"""
                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{FieldModifier}}{{KeyTypeName}}[] _keys = {
                    {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                        };

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetEarlyExits(MethodType.Contains)}}

                            return {{GetEqualFunction("key", $"_keys[key.Length - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}} value)
                        {
                            value = default;
                        {{GetEarlyExits(MethodType.TryLookup)}}

                            int idx = key.Length - {{ctx.MinLength.ToStringInvariant()}};
                            if ({{GetEqualFunction("key", "_keys[idx]")}})
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