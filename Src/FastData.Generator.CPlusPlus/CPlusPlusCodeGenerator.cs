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

    public override string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext<TValue> context)
    {
        //C++ generator does not support chars outside ASCII
        if (genCfg.DataType == DataType.Char && (char)(object)genCfg.Constants.MaxValue > 127)
            throw new InvalidOperationException("C++ generator does not support chars outside ASCII. Please use a different data type or reduce the max value to 127 or lower.");

        return base.Generate(genCfg, context);
    }

    protected override void AppendHeader<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, IContext<TValue> context)
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

    protected override OutputWriter<TKey>? GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext<TValue> context) => context switch
    {
        SingleValueContext<TKey, TValue> x => new SingleValueCode<TKey, TValue>(x, _cfg.ClassName),
        ArrayContext<TKey, TValue> x => new ArrayCode<TKey, TValue>(x, Shared, _cfg.ClassName),
        BinarySearchContext<TKey, TValue> x => new BinarySearchCode<TKey, TValue>(x, _cfg.ClassName),
        ConditionalContext<TKey, TValue> x => new ConditionalCode<TKey, TValue>(x, _cfg.ClassName),
        HashTableChainContext<TKey, TValue> x => new HashTableChainCode<TKey, TValue>(x, _cfg.ClassName),
        HashTablePerfectContext<TKey, TValue> x => new HashTablePerfectCode<TKey, TValue>(x, _cfg.ClassName),
        KeyLengthContext<TValue> x => new KeyLengthCode<TKey, TValue>(x, _cfg.ClassName),
        _ => null
    };
}