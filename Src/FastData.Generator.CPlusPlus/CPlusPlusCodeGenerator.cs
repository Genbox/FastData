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
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);

        return new CPlusPlusCodeGenerator(userCfg, langDef, new CPlusPlusConstantsDef(), new CPlusPlusEarlyExitDef(map, userCfg.GeneratorOptions), new CPlusPlusHashDef(), map);
    }

    public override GeneratorEncoding Encoding => GeneratorEncoding.UTF8;

    public override string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context)
    {
        //C++ generator does not support chars outside ASCII
        if (genCfg.KeyType == KeyType.Char && (char)(object)genCfg.Constants.MaxValue > 127)
            throw new InvalidOperationException("C++ generator does not support chars outside ASCII. Please use a different data type or reduce the max value to 127 or lower.");

        return base.Generate<TKey, TValue>(genCfg, context);
    }

    protected override void AppendHeader<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, IContext context)
    {
        base.AppendHeader<TKey, TValue>(sb, genCfg, context);

        sb.AppendLine("""
                      #pragma once
                      #include <array>
                      #include <cstring>
                      #include <cstdint>
                      #include <limits>
                      #include <string_view>

                      """);
    }

    protected override void AppendBody<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, string keyTypeName, string valueTypeName, IContext context)
    {
        sb.AppendLine($$"""
                        class {{_cfg.ClassName}} final {
                        """);

        base.AppendBody<TKey, TValue>(sb, genCfg, keyTypeName, valueTypeName, context);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);
        sb.Append("};");
    }

    protected override OutputWriter<TKey>? GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context) => context switch
    {
        SingleValueContext<TKey, TValue> x => new SingleValueCode<TKey, TValue>(x, Shared),
        RangeContext<TKey> x => new RangeCode<TKey, TValue>(x),
        BitSetContext<TValue> x => new BitSetCode<TKey, TValue>(x, Shared),
        BloomFilterContext x => new BloomFilterCode<TKey>(x),
        ArrayContext<TKey, TValue> x => new ArrayCode<TKey, TValue>(x, Shared),
        BinarySearchContext<TKey, TValue> x => new BinarySearchCode<TKey, TValue>(x, Shared),
        ConditionalContext<TKey, TValue> x => new ConditionalCode<TKey, TValue>(x, Shared),
        HashTableContext<TKey, TValue> x => new HashTableCode<TKey, TValue>(x, Shared),
        HashTableCompactContext<TKey, TValue> x => new HashTableCompactCode<TKey, TValue>(x, Shared),
        HashTablePerfectContext<TKey, TValue> x => new HashTablePerfectCode<TKey, TValue>(x, Shared),
        KeyLengthContext<TValue> x => new KeyLengthCode<TKey, TValue>(x, Shared),
        _ => null
    };
}