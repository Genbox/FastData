using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CPlusPlus.Internal.Generators;
using Genbox.FastData.Models;

namespace Genbox.FastData.Generator.CPlusPlus;

public class CPlusPlusCodeGenerator(CPlusPlusGeneratorConfig userCfg) : IGenerator
{
    private readonly StringBuilder _sb = new StringBuilder();

    public string Generate(GeneratorConfig genCfg, FastDataConfig fastCfg, IContext context)
    {
        _sb.Clear();
        AppendHeader(fastCfg);

        _sb.Append(context switch
        {
            SingleValueContext c2 => new SingleValueCode(genCfg, userCfg, c2).Generate(),
            ArrayContext c1 => new ArrayCode(genCfg, userCfg, c1).Generate(),
            BinarySearchContext c2 => new BinarySearchCode(genCfg, userCfg, c2).Generate(),
            ConditionalContext c2 => new ConditionalCode(genCfg, userCfg, c2).Generate(),
            EytzingerSearchContext c2 => new EytzingerSearchCode(genCfg, userCfg, c2).Generate(),
            PerfectHashBruteForceContext c2 => new PerfectHashBruteForceCode(genCfg, userCfg, c2).Generate(),
            PerfectHashGPerfContext c2 => new PerfectHashGPerfCode(genCfg, userCfg, c2).Generate(),
            HashSetChainContext c2 => new HashSetChainCode(genCfg, userCfg, c2).Generate(),
            HashSetLinearContext c2 => new HashSetLinearCode(genCfg, userCfg, c2).Generate(),
            KeyLengthContext c2 => new KeyLengthCode(genCfg, userCfg, c2).Generate(),
            _ => throw new NotSupportedException("The context type is not supported: " + context.GetType().Name)
        });

        AppendFooter(fastCfg);
        return _sb.ToString();
    }

    private void AppendHeader(FastDataConfig fastCfg)
    {
        string cn = fastCfg.Name;

        _sb.Append($$"""
                     #include <string>
                     #include <array>
                     #include <cstdint>

                     class {{cn}}
                     {

                     """);
    }

    private void AppendFooter(FastDataConfig fastCfg)
    {
        _sb.Append($"""


                        static constexpr int item_count = {fastCfg.Data.Length};
                    """);

        _sb.AppendLine();
        _sb.Append("};");
    }
}