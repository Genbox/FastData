using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class KeyLengthCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, KeyLengthContext ctx) : IOutputWriter
{
    public string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths
                               .Skip((int)ctx.MinLength)
                               .Select(x => x?.FirstOrDefault())
                               .ToArray();

        return $$"""
                     {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName()}}; {{lengths.Length}}] = [
                 {{FormatColumns(lengths, ToValueLabel)}}
                     ];

                     #[must_use]
                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                 {{GetEarlyExit(genCfg.EarlyExits)}}
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
                     {{cfg.GetFieldModifier()}}const ENTRIES: [Vec<{{genCfg.GetTypeName()}}>; {{lengths.Length}}] = [
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     ];

                     #[must_use]
                     {{cfg.GetMethodModifier()}}fn contains(value: &{{genCfg.GetTypeName()}}) -> bool {
                 {{GetEarlyExit(genCfg.EarlyExits)}}
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

    private string GetEarlyExit(IEarlyExit[] exits)
    {
        // unchanged
        MinMaxLengthEarlyExit? exit1 = (MinMaxLengthEarlyExit?)Array.Find(exits, x => x is MinMaxLengthEarlyExit);
        if (exit1 != null)
            return RustGeneratorConfigExtensions.GetLengthEarlyExits(exit1.MinValue, exit1.MaxValue);

        MinMaxValueEarlyExit? exit2 = (MinMaxValueEarlyExit?)Array.Find(exits, x => x is MinMaxValueEarlyExit);
        if (exit2 != null)
            return RustGeneratorConfigExtensions.GetValueEarlyExits(exit2.MinValue, exit2.MaxValue, genCfg.DataType);

        LengthBitSetEarlyExit? exit3 = (LengthBitSetEarlyExit?)Array.Find(exits, x => x is LengthBitSetEarlyExit);
        if (exit3 != null)
            return RustGeneratorConfigExtensions.GetMaskEarlyExit(exit3.BitSet);

        throw new InvalidOperationException("No early exits were found. They are required for UniqueKeyLength");
    }

    private static string RenderMany(List<string>? x)
    {
        if (x == null)
            return "Vec::new()";

        return $"vec![{string.Join(", ", x.Select(ToValueLabel))}]";
    }
}