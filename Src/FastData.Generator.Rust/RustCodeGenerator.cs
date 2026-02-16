using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generator.Rust.Internal.Generators;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust;

public sealed class RustCodeGenerator : CodeGenerator
{
    private readonly RustCodeGeneratorConfig _cfg;

    private RustCodeGenerator(RustCodeGeneratorConfig cfg, ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef, TypeMap map)
        : base(langDef, constDef, earlyExitDef, hashDef, map, null) => _cfg = cfg;

    public static RustCodeGenerator Create(RustCodeGeneratorConfig userCfg)
    {
        RustLanguageDef langDef = new RustLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);

        return new RustCodeGenerator(userCfg, langDef, new RustConstantsDef(), new RustEarlyExitDef(map, userCfg.GeneratorOptions), new RustHashDef(), map);
    }

    public override GeneratorEncoding Encoding => GeneratorEncoding.UTF8;

    protected override void AppendHeader<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, IContext context)
    {
        base.AppendHeader<TKey, TValue>(sb, genCfg, context);

        sb.Append($"""
                   #![allow(unused_parens)]
                   #![allow(missing_docs)]
                   #![allow(unused_imports)]
                   #![allow(unused_unsafe)]
                   use std::ptr;

                   pub struct {_cfg.ClassName};

                   """);
    }

    protected override void AppendBody<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, string keyTypeName, string valueTypeName, IContext context)
    {
        sb.Append($$"""

                    impl {{_cfg.ClassName}} {

                    """);

        base.AppendBody<TKey, TValue>(sb, genCfg, keyTypeName, valueTypeName, context);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);

        sb.Append('}');
    }

    protected override OutputWriter<TKey>? GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context) => context switch
    {
        SingleValueContext<TKey, TValue> x => new SingleValueCode<TKey, TValue>(x, Shared),
        RangeContext<TKey> x => new RangeCode<TKey>(x),
        BitSetContext<TValue> x => new BitSetCode<TKey, TValue>(x, Shared),
        BloomFilterContext x => new BloomFilterCode<TKey>(x),
        ArrayContext<TKey, TValue> x => new ArrayCode<TKey, TValue>(x, Shared),
        BinarySearchContext<TKey, TValue> x => new BinarySearchCode<TKey, TValue>(x, Shared),
        ConditionalContext<TKey, TValue> x => new ConditionalCode<TKey, TValue>(x, Shared),
        HashTableContext<TKey, TValue> x => new HashTableCode<TKey, TValue>(x, genCfg, Shared),
        HashTableCompactContext<TKey, TValue> x => new HashTableCompactCode<TKey, TValue>(x, genCfg, Shared),
        HashTablePerfectContext<TKey, TValue> x => new HashTablePerfectCode<TKey, TValue>(x, genCfg, Shared),
        KeyLengthContext<TValue> x => new KeyLengthCode<TKey, TValue>(x, Shared),
        _ => null
    };
}