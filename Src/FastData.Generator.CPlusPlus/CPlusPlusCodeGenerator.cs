using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.Internal.Generators;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.CPlusPlus;

public class CPlusPlusCodeGenerator : CodeGenerator
{
    private readonly CPlusPlusCodeGeneratorConfig _userCfg;

    private CPlusPlusCodeGenerator(CPlusPlusCodeGeneratorConfig userCfg,
                                   ILanguageSpec langSpec,
                                   ICodeSpec codeSpec,
                                   IConstantsSpec constants,
                                   IEarlyExitHandler earlyExitHandler,
                                   IHashHandler hashHandler) : base(langSpec, codeSpec, constants, earlyExitHandler, hashHandler)
    {
        _userCfg = userCfg;
    }

    public static CPlusPlusCodeGenerator Create(CPlusPlusCodeGeneratorConfig userCfg)
    {
        CPlusPlusLanguageSpec langSpec = new CPlusPlusLanguageSpec();
        TypeMap typeMap = new TypeMap(langSpec.Primitives);
        CodeHelper helper = new CodeHelper(langSpec, typeMap);

        return new CPlusPlusCodeGenerator(userCfg, langSpec, new CPlusPlusCodeSpec(), new CPlusPlusConstantsSpec(), new CPlusPlusEarlyExitHandler(helper, userCfg.GeneratorOptions), new CPlusPlusHashHandler());
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

                    class {{_userCfg.ClassName}} final
                    {

                    """);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);

        string cn = _userCfg.ClassName;
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
        PerfectHashBruteForceContext<T> x => new PerfectHashBruteForceCode<T>(x),
        PerfectHashGPerfContext x => new PerfectHashGPerfCode<T>(x, genCfg),
        HashSetChainContext<T> x => new HashSetChainCode<T>(x),
        HashSetLinearContext<T> x => new HashSetLinearCode<T>(x),
        KeyLengthContext x => new KeyLengthCode<T>(x),
        _ => null
    };
}