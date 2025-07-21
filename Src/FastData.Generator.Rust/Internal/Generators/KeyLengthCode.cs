using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        string?[] lengths = ctx.Lengths;

        return $$"""
                     {{FieldModifier}}const ENTRIES: [{{KeyTypeName}}; {{lengths.Length.ToStringInvariant()}}] = [
                 {{FormatColumns(lengths, ToValueLabel)}}
                     ];

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
                 {{EarlyExits}}
                         return Self::ENTRIES[(key.len() - {{ctx.MinLength.ToStringInvariant()}}) as usize] == key;
                     }
                 """;
    }
}