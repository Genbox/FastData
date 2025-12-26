using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustTestHarness : TestHarnessBase
{
    private readonly RustCompiler _compiler;

    public RustTestHarness() : base("Rust")
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "Rust");
        Directory.CreateDirectory(rootDir);
        _compiler = new RustCompiler(false, rootDir);
    }

    public override ICodeGenerator CreateGenerator(string id) => RustCodeGenerator.Create(new RustCodeGeneratorConfig(id));

    public override ITestRenderer CreateRenderer(GeneratorSpec spec) => new RustRenderer(spec);

    public override string RenderContainsProgram<T>(GeneratorSpec spec, ITestRenderer renderer, T[] present, T[] notPresent)
    {
        string presentChecks = FormatHelper.FormatList(present, x => $$"""
                                                                           if !{{spec.Identifier}}::contains({{renderer.ToValueLabel(x)}}) {
                                                                               std::process::exit(0);
                                                                           }
                                                                       """, "\n");

        string notPresentChecks = FormatHelper.FormatList(notPresent, x => $$"""
                                                                                 if {{spec.Identifier}}::contains({{renderer.ToValueLabel(x)}}) {
                                                                                     std::process::exit(0);
                                                                                 }
                                                                             """, "\n");

        return $$"""
                 #![allow(non_camel_case_types)]
                 {{spec.Source}}

                 fn main() {
                 {{presentChecks}}

                 {{notPresentChecks}}

                     std::process::exit(1);
                 }
                 """;
    }

    public override string RenderTryLookupProgram<TKey, TValue>(GeneratorSpec spec, ITestRenderer renderer, TestVector<TKey, TValue> vector)
    {
        string checks = FormatHelper.FormatList(vector.Keys, x => $$"""
                                                                        if {{spec.Identifier}}::try_lookup({{renderer.ToValueLabel(x)}}).is_none() {
                                                                            std::process::exit(0);
                                                                        }
                                                                    """, "\n");

        return $$"""
                 #![allow(non_camel_case_types)]
                 {{spec.Source}}

                 fn main() {
                 {{checks}}

                     std::process::exit(1);
                 }
                 """;
    }

    private sealed class RustRenderer : ITestRenderer
    {
        private readonly TypeMap _map;

        public RustRenderer(GeneratorSpec spec)
        {
            RustLanguageDef langDef = new RustLanguageDef();
            Encoding = spec.Flags.HasFlag(GeneratorFlags.AllAreASCII) ? GeneratorEncoding.ASCII : langDef.Encoding;
            _map = new TypeMap(langDef.TypeDefinitions, Encoding);
        }

        public GeneratorEncoding Encoding { get; }

        public string ToValueLabel<T>(T value) => _map.ToValueLabel(value);

        public string GetTypeName(Type type) => _map.GetTypeName(type);
    }

    public override int Run(string fileId, string source)
    {
        string executable = _compiler.Compile(fileId, source);
        return RunProcess(executable).ExitCode;
    }
}