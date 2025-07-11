using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx) : RustOutputWriter<TKey>
{
    public override string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths
                               .Skip((int)ctx.MinLength)
                               .Select(x => x?.FirstOrDefault())
                               .ToArray();

        return $$"""
                     {{FieldModifier}}const ENTRIES: [{{KeyTypeName}}; {{lengths.Length.ToStringInvariant()}}] = [
                 {{FormatColumns(lengths, ToValueLabel)}}
                     ];

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(value: {{KeyTypeName}}) -> bool {
                 {{EarlyExits}}
                         return Self::ENTRIES[(value.len() - {{ctx.MinLength.ToStringInvariant()}}) as usize] == value;
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths
                                     .Skip((int)ctx.MinLength)
                                     .Take((int)(ctx.MaxLength - ctx.MinLength + 1))
                                     .ToArray();

        return $$"""
                     {{FieldModifier}}const ENTRIES: [Vec<{{KeyTypeName}}>; {{lengths.Length}}] = [
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     ];

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(value: &{{KeyTypeName}}) -> bool {
                 {{EarlyExits}}
                         let idx = (value.len() - {{ctx.MinLength.ToStringInvariant()}}) as usize;
                         let bucket = &Self::ENTRIES[idx];

                         if bucket.is_empty() {
                             false
                         }

                         for item in bucket.iter() {
                             if item == value {
                                 true
                             }
                         }

                         false
                     }
                 """;
    }

    private string RenderMany(List<string>? x) => x == null ? "Vec::new()" : $"vec![{string.Join(", ", x.Select(ToValueLabel))}]";
}