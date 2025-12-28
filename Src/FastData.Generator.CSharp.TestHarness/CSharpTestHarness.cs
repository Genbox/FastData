using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpTestHarness() : TestHarnessBase("CSharp")
{
    public override ICodeGenerator CreateGenerator(string id) => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id));

    public override ITestRenderer CreateRenderer(GeneratorSpec spec) => new CSharpRenderer();

    public override string RenderContainsProgram<T>(GeneratorSpec spec, ITestRenderer renderer, T[] present, T[] notPresent)
    {
        string presentChecks = FormatHelper.FormatList(present, x => $"""
                                                                          if (!{spec.Identifier}.Contains({renderer.ToValueLabel(x)}))
                                                                              return 0;
                                                                      """, "\n");

        string notPresentChecks = FormatHelper.FormatList(notPresent, x => $"""
                                                                                if ({spec.Identifier}.Contains({renderer.ToValueLabel(x)}))
                                                                                    return 0;
                                                                            """, "\n");

        return $$"""
                 {{spec.Source}}

                 public static class Program
                 {
                     public static int Main()
                     {
                 {{presentChecks}}

                 {{notPresentChecks}}

                         return 1;
                     }
                 }
                 """;
    }

    public override string RenderTryLookupProgram<TKey, TValue>(GeneratorSpec spec, ITestRenderer renderer, TestVector<TKey, TValue> vector)
    {
        string checks = FormatHelper.FormatList(vector.Keys, x => $"""
                                                                       if (!{spec.Identifier}.TryLookup({renderer.ToValueLabel(x)}, out _))
                                                                           return 0;
                                                                   """, "\n");

        return $$"""
                 {{spec.Source}}

                 public static class Program
                 {
                     public static int Main()
                     {
                 {{checks}}

                         return 1;
                     }
                 }
                 """;
    }

    private sealed class CSharpRenderer : ITestRenderer
    {
        private readonly TypeMap _map;

        public CSharpRenderer()
        {
            CSharpLanguageDef langDef = new CSharpLanguageDef();
            _map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.UTF16);
        }

        public string ToValueLabel<T>(T value) => _map.ToValueLabel(value);

        public string GetTypeName(Type type) => _map.GetTypeName(type);
    }

    public override int Run(string fileId, string source)
    {
        Func<int> main = CompilationHelper.GetDelegate<Func<int>>(source, types => types.First(x => x.Name == "Program"), methods => methods.First(x => x.Name == "Main"), false);
        return main();
    }
}