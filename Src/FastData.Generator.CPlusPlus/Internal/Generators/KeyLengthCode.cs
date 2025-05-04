using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class KeyLengthCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, KeyLengthContext ctx) : IOutputWriter
{
    public string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()).ToArray();

        return $$"""
                     {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName()}}, {{lengths.Length}}> entries = {
                 {{FormatColumns(lengths, ToValueLabel)}}
                     };

                 public:
                     [[nodiscard]]
                     {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
                     {
                 {{GetEarlyExit(genCfg.EarlyExits)}}

                         return value == entries[value.length() - {{ctx.MinLength.ToStringInvariant()}}];
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Take((int)((ctx.MaxLength - ctx.MinLength) + 1)).ToArray();

        return $$"""
                     {{cfg.GetFieldModifier()}}std:array<std:vector<{{genCfg.GetTypeName()}}>, {{lengths.Length}}> entries = {
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     };

                 public:
                     [[nodiscard]]
                     {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}}& value) noexcept
                     {
                 {{GetEarlyExit(genCfg.EarlyExits)}}
                         std::vector<{{genCfg.GetTypeName()}}> bucket = entries[value.length() - {{ctx.MinLength}}];

                         if (bucket == nullptr)
                             return false;

                         foreach ({{genCfg.GetTypeName()}} str in bucket)
                         {
                             if (str == value)
                                 return true;
                         }

                         return false;
                     }
                 """;
    }

    private string GetEarlyExit(IEarlyExit[] exits)
    {
        //We do this to force an early exit for this data structure. Otherwise, we will get an IndexOutOfRangeException
        MinMaxLengthEarlyExit? exit1 = (MinMaxLengthEarlyExit?)Array.Find(exits, x => x is MinMaxLengthEarlyExit);
        if (exit1 != null)
            return CPlusPlusGeneratorConfigExtensions.GetLengthEarlyExits(exit1.MinValue, exit1.MaxValue);

        MinMaxValueEarlyExit? exit2 = (MinMaxValueEarlyExit?)Array.Find(exits, x => x is MinMaxValueEarlyExit);
        if (exit2 != null)
            return CPlusPlusGeneratorConfigExtensions.GetValueEarlyExits(exit2.MinValue, exit2.MaxValue, genCfg.DataType);

        LengthBitSetEarlyExit? exit3 = (LengthBitSetEarlyExit?)Array.Find(exits, x => x is LengthBitSetEarlyExit);
        if (exit3 != null)
            return CPlusPlusGeneratorConfigExtensions.GetMaskEarlyExit(exit3.BitSet);

        throw new InvalidOperationException("No early exits were found. They are required for UniqueKeyLength");
    }

    private static string RenderMany(List<string>? x)
    {
        if (x == null)
            return "        \"\"";

        return $"new [] {{ {string.Join(",", x.Select(ToValueLabel))} }}";
    }
}