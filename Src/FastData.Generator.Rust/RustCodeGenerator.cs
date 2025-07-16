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
        TypeMap map = new TypeMap(langDef.TypeDefinitions, langDef.Encoding);

        return new RustCodeGenerator(userCfg, langDef, new RustConstantsDef(), new RustEarlyExitDef(map, userCfg.GeneratorOptions), new RustHashDef(), map);
    }

    protected override void AppendHeader<TKey, TValue>(StringBuilder sb, GeneratorConfig<TKey> genCfg, IContext<TValue> context)
    {
        base.AppendHeader(sb, genCfg, context);

        sb.Append($$"""
                    #![allow(unused_parens)]
                    #![allow(missing_docs)]
                    #![allow(unused_imports)]
                    #![allow(unused_unsafe)]
                    use std::ptr;

                    pub struct {{_cfg.ClassName}};

                    impl {{_cfg.ClassName}} {

                    """);
    }

    protected override void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        base.AppendFooter(sb, genCfg, typeName);

        sb.Append('}');
    }

    protected override OutputWriter<TKey>? GetOutputWriter<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext<TValue> context) => context switch
    {
        SingleValueContext<TKey, TValue> x => new SingleValueCode<TKey, TValue>(x),
        ArrayContext<TKey, TValue> x => new ArrayCode<TKey, TValue>(x),
        BinarySearchContext<TKey, TValue> x => new BinarySearchCode<TKey, TValue>(x),
        ConditionalContext<TKey, TValue> x => new ConditionalCode<TKey, TValue>(x),
        HashTableChainContext<TKey, TValue> x => new HashTableChainCode<TKey, TValue>(x, genCfg, Shared),
        HashTablePerfectContext<TKey, TValue> x => new HashTablePerfectCode<TKey, TValue>(x, genCfg, Shared),
        KeyLengthContext<TValue> x => new KeyLengthCode<TKey, TValue>(x),
        _ => null
    };
}