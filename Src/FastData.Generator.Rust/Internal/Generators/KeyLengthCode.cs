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

        shared.Add(CodePlacement.Before, $"""

                                             {FieldModifier} static KEYS: [{GetKeyTypeName(customKey)}; {ctx.Lengths.Length.ToStringInvariant()}] = [
                                          {FormatColumns(ctx.Lengths, ToValueLabel)}
                                              ];
                                          """);

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetEarlyExits(MethodType.Contains)}}

                            return {{GetEqualFunction("key", $"KEYS[key.len() - {ctx.MinLength.ToStringInvariant()}]")}};
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            shared.Add(CodePlacement.Before, $"""

                                                 {FieldModifier} static VALUES: [{GetValueTypeName(customValue)}; {ctx.Values.Length.ToStringInvariant()}] = [
                                              {FormatColumns(ctx.Values, ToValueLabel)}
                                                  ];
                                              """);

            shared.Add(CodePlacement.Before, $"""

                                                 {FieldModifier} static OFFSETS: [i32; {ctx.Values.Length.ToStringInvariant()}] = [
                                              {FormatColumns(ctx.ValueOffsets, static x => x.ToStringInvariant())}
                                                  ];
                                              """);

            sb.Append($$"""

                        {{MethodAttribute}}
                        {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetEarlyExits(MethodType.TryLookup)}}

                            let idx = (key.len() - {{ctx.MinLength.ToStringInvariant()}}) as usize;
                            if ({{GetEqualFunction("key", "KEYS[idx]")}}) {
                                return Some(VALUES[OFFSETS[idx] as usize]);
                            }
                            None
                        }
                        """);
        }

        return sb.ToString();
    }
}