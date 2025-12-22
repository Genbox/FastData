using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""
                          {FieldModifier}VALUES: [{GetValueTypeName(customValue)}; {ctx.Values.Length.ToStringInvariant()}] = [
                       {FormatColumns(ctx.Values, ToValueLabel)}
                           ];

                          {FieldModifier}OFFSETS: [i32; {ctx.Values.Length.ToStringInvariant()}] = [
                       {FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}
                           ];

                       """);
        }

        sb.Append($$"""
                        {{FieldModifier}}KEYS: [{{GetKeyTypeName(customKey)}}; {{ctx.Lengths.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                        ];

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            return {{GetEqualFunction(LookupKeyName, $"Self::KEYS[{LookupKeyName}.len() - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""

                        {{MethodAttribute}}
                        {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                            let idx = ({{LookupKeyName}}.len() - {{ctx.MinLength.ToStringInvariant()}}) as usize;
                            if ({{GetEqualFunction(LookupKeyName, "Self::KEYS[idx]")}}) {
                                return Some(Self::VALUES[Self::OFFSETS[idx] as usize]);
                            }
                            None
                        }
                        """);
        }

        return sb.ToString();
    }
}