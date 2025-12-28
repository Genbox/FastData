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

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""
                          {FieldModifier}VALUES: [{GetValueTypeName(customValue)}; {values.Length.ToStringInvariant()}] = [
                       {FormatColumns(values, ToValueLabel)}
                           ];

                          {FieldModifier}OFFSETS: [i32; {values.Length.ToStringInvariant()}] = [
                       {FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}
                           ];

                       """);
        }

        sb.Append($$"""
                        {{FieldModifier}}KEYS: [{{GetKeyTypeName(customKey)}}; {{ctx.Lengths.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.Lengths, ToValueLabel)}}
                        ];

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            return {{GetEqualFunction(LookupKeyName, $"Self::KEYS[{LookupKeyName}.len() - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                        {{MethodAttribute}}
                        {{MethodModifier}}fn try_lookup({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
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