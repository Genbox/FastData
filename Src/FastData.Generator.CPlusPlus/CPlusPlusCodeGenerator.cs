using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.CPlusPlus.Internal.Generators;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus;

public sealed class CPlusPlusCodeGenerator : CodeGenerator
{
    private readonly CPlusPlusCodeGeneratorConfig _cfg;

    private CPlusPlusCodeGenerator(CPlusPlusCodeGeneratorConfig cfg, ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef, TypeMap helper)
        : base(langDef, constDef, earlyExitDef, hashDef, helper, null) => _cfg = cfg;

    public static CPlusPlusCodeGenerator Create(CPlusPlusCodeGeneratorConfig userCfg)
    {
        CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, langDef.Encoding);

        return new CPlusPlusCodeGenerator(userCfg, langDef, new CPlusPlusConstantsDef(), new CPlusPlusEarlyExitDef(map, userCfg.GeneratorOptions), new CPlusPlusHashDef(), map);
    }

    public override string Generate<T>(ReadOnlySpan<T> data, GeneratorConfig<T> genCfg, IContext<T> context)
    {
        //C++ generator does not support chars outside ASCII
        if (genCfg.DataType == DataType.Char && (char)(object)genCfg.Constants.MaxValue > 127)
            throw new InvalidOperationException("C++ generator does not support chars outside ASCII. Please use a different data type or reduce the max value to 127 or lower.");

        return base.Generate(data, genCfg, context);
    }

    protected override void AppendHeader<T>(StringBuilder sb, GeneratorConfig<T> genCfg, IContext<T> context)
    {
        base.AppendHeader(sb, genCfg, context);

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

    protected override OutputWriter<T>? GetOutputWriter<T>(GeneratorConfig<T> genCfg, IContext<T> context) => context switch
    {
        SingleValueContext<T> x => new SingleValueCode<T>(x),
        ArrayContext<T> x => new ArrayCode<T>(x),
        BinarySearchContext<T> x => new BinarySearchCode<T>(x),
        ConditionalContext<T> x => new ConditionalCode<T>(x),
        EytzingerSearchContext<T> x => new EytzingerSearchCode<T>(x),
        HashSetChainContext<T> x => new HashSetChainCode<T>(x),
        HashSetLinearContext<T> x => new HashSetLinearCode<T>(x),
        HashSetPerfectContext<T> x => new HashSetPerfectCode<T>(x),
        KeyLengthContext<T> x => new KeyLengthCode<T>(x),
        _ => null
    };
}