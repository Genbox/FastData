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

    private RustCodeGenerator(RustCodeGeneratorConfig cfg, ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef)
        : base(langDef, constDef, earlyExitDef, hashDef, null) => _cfg = cfg;

    public static RustCodeGenerator Create(RustCodeGeneratorConfig userCfg)
    {
        RustLanguageDef langDef = new RustLanguageDef();
        TypeHelper helper = new TypeHelper(new TypeMap(langDef.TypeDefinitions));

        return new RustCodeGenerator(userCfg, langDef, new RustConstantsDef(), new RustEarlyExitDef(helper, userCfg.GeneratorOptions), new RustHashDef());
    }

    protected override void AppendHeader<T>(StringBuilder sb, GeneratorConfig<T> genCfg, IContext<T> context)
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

    protected override OutputWriter<T>? GetOutputWriter<T>(GeneratorConfig<T> genCfg, IContext<T> context) => context switch
    {
        SingleValueContext<T> x => new SingleValueCode<T>(x),
        ArrayContext<T> x => new ArrayCode<T>(x),
        BinarySearchContext<T> x => new BinarySearchCode<T>(x),
        ConditionalContext<T> x => new ConditionalCode<T>(x),
        EytzingerSearchContext<T> x => new EytzingerSearchCode<T>(x),
        HashSetChainContext<T> x => new HashSetChainCode<T>(x, genCfg, Shared),
        HashSetLinearContext<T> x => new HashSetLinearCode<T>(x, genCfg, Shared),
        HashSetPerfectContext<T> x => new HashSetPerfectCode<T>(x, genCfg, Shared),
        KeyLengthContext<T> x => new KeyLengthCode<T>(x),
        _ => null
    };
}