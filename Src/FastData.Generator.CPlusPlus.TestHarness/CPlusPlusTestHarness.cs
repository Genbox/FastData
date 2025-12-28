using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusTestHarness : TestHarnessBase
{
    private readonly CPlusPlusCompiler _compiler;

    public CPlusPlusTestHarness() : base("CPlusPlus")
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");
        Directory.CreateDirectory(rootDir);
        _compiler = new CPlusPlusCompiler(false, rootDir);
    }

    public override ICodeGenerator CreateGenerator(string id) => CPlusPlusCodeGenerator.Create(new CPlusPlusCodeGeneratorConfig(id));

    public override ITestRenderer CreateRenderer(GeneratorSpec spec) => new CPlusPlusRenderer();

    public override string RenderContainsProgram<T>(GeneratorSpec spec, ITestRenderer renderer, T[] present, T[] notPresent)
    {
        string presentChecks = FormatHelper.FormatList(present, x => $"""
                                                                          if (!{spec.Identifier}::contains({renderer.ToValueLabel(x)}))
                                                                              return 0;
                                                                      """, "\n");

        string notPresentChecks = FormatHelper.FormatList(notPresent, x => $"""
                                                                                if ({spec.Identifier}::contains({renderer.ToValueLabel(x)}))
                                                                                    return 0;
                                                                            """, "\n");

        return $$"""
                 #include <string>
                 #include <iostream>

                 {{spec.Source}}

                 int main()
                 {
                 {{presentChecks}}

                 {{notPresentChecks}}

                     return 1;
                 }
                 """;
    }

    public override string RenderTryLookupProgram<TKey, TValue>(GeneratorSpec spec, ITestRenderer renderer, TestVector<TKey, TValue> vector)
    {
        string valueType = renderer.GetTypeName(vector.Values[0].GetType());
        string checks = FormatHelper.FormatList(vector.Keys, x => $"""
                                                                       if (!{spec.Identifier}::try_lookup({renderer.ToValueLabel(x)}, res))
                                                                           return 0;
                                                                   """, "\n");

        return $$"""
                 #include <string>
                 #include <iostream>

                 {{spec.Source}}

                 int main()
                 {
                     const {{valueType}}* res;
                 {{checks}}

                     return 1;
                 }
                 """;
    }

    public override int Run(string fileId, string source)
    {
        string executable = _compiler.Compile(fileId, source);
        return RunProcess(executable).ExitCode;
    }

    private sealed class CPlusPlusRenderer : ITestRenderer
    {
        private readonly TypeMap _map;

        public CPlusPlusRenderer()
        {
            CPlusPlusLanguageDef langDef = new CPlusPlusLanguageDef();
            _map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF8);
        }

        public string ToValueLabel<T>(T value) => _map.ToValueLabel(value);

        public string GetTypeName(Type type) => _map.GetTypeName(type);
    }
}