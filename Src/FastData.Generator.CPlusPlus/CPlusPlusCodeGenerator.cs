using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.Internal.Generators;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusCodeGenerator : CodeGenerator
{
    private readonly CPlusPlusCodeGeneratorConfig _cfg;

    private CPlusPlusCodeGenerator(CPlusPlusCodeGeneratorConfig cfg, ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef)
        : base(langDef, constDef, earlyExitDef, hashDef) => _cfg = cfg;

    public static CPlusPlusCodeGenerator Create(CPlusPlusCodeGeneratorConfig userCfg)
    {
        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeHelper helper = new TypeHelper(new TypeMap(langDef.TypeDefinitions));

        return new CPlusPlusCodeGenerator(userCfg, langDef, new CPlusPlusConstantsDef(), new CPlusPlusEarlyExitDef(helper, userCfg.GeneratorOptions), new CPlusPlusHashDef());
    }

    public override bool TryGenerate<T>(GeneratorConfig<T> genCfg, IContext context, out string? source)
    {
        //C++ generator does not support chars outside ASCII
        if (genCfg.DataType == DataType.Char && (char)(object)genCfg.Constants.MaxValue > 127)
        {
            source = null;
            return false;
        }

        return base.TryGenerate(genCfg, context, out source);
    }

    protected override void AppendHeader<T>(StringBuilder sb, GeneratorConfig<T> genCfg)
    {
        base.AppendHeader(sb, genCfg);

        sb.AppendLine("#pragma once"); //Add include guard

        sb.Append($$"""
                    #include <array>
                    #include <cstdint>
                    #include <limits>
                    #include <string_view>

                    class {{_cfg.ClassName}} final
                    {

                    """);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);

        string cn = _cfg.ClassName;
        sb.Append($$"""

                    public:
                        {{cn}}() = delete;
                        {{cn}}(const {{cn}}&) = delete;
                        {{cn}}& operator=(const {{cn}}&) = delete;
                        {{cn}}({{cn}}&&) = delete;
                        {{cn}}& operator=({{cn}}&&) = delete;
                    };
                    """);
    }

    protected override OutputWriter<T>? GetOutputWriter<T>(GeneratorConfig<T> genCfg, IContext context) => context switch
    {
        SingleValueContext<T> x => new SingleValueCode<T>(x),
        ArrayContext<T> x => new ArrayCode<T>(x),
        BinarySearchContext<T> x => new BinarySearchCode<T>(x),
        ConditionalContext<T> x => new ConditionalCode<T>(x),
        EytzingerSearchContext<T> x => new EytzingerSearchCode<T>(x),
        PerfectHashGPerfContext x => new PerfectHashGPerfCode<T>(x, genCfg),
        HashSetChainContext<T> x => new HashSetChainCode<T>(x),
        HashSetLinearContext<T> x => new HashSetLinearCode<T>(x),
        HashSetPerfectContext<T> x => new HashSetPerfectCode<T>(x),
        KeyLengthContext x => new KeyLengthCode<T>(x),
        _ => null
    };
}