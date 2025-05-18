using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class KeyLengthCode<T>(KeyLengthContext ctx) : RustOutputWriter<T>
{
    public override string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths
                               .Skip((int)ctx.MinLength)
                               .Select(x => x?.FirstOrDefault())
                               .ToArray();

        return $$"""
                     {{GetFieldModifier()}}const ENTRIES: [{{TypeName}}; {{lengths.Length}}] = [
                 {{FormatColumns(lengths, ToValueLabel)}}
                     ];

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
                 {{GetEarlyExits()}}
                         return Self::ENTRIES[(value.len() - {{ctx.MinLength.ToStringInvariant()}}) as usize] == value;
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths
                                     .Skip((int)ctx.MinLength)
                                     .Take((int)((ctx.MaxLength - ctx.MinLength) + 1))
                                     .ToArray();

        return $$"""
                     {{GetFieldModifier()}}const ENTRIES: [Vec<{{TypeName}}>; {{lengths.Length}}] = [
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     ];

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: &{{TypeName}}) -> bool {
                 {{GetEarlyExits()}}
                         let idx = (value.len() - {{ctx.MinLength}}) as usize;
                         let bucket = &Self::ENTRIES[idx];

                         if bucket.is_empty() {
                             return false;
                         }

                         for item in bucket.iter() {
                             if item == value {
                                 return true;
                             }
                         }

                         false
                     }
                 """;
    }

    private string RenderMany(List<string>? x)
    {
        if (x == null)
            return "Vec::new()";

        return $"vec![{string.Join(", ", x.Select(ToValueLabel))}]";
    }
}