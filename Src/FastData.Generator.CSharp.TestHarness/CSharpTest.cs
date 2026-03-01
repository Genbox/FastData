using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpTest() : TestBase<CSharpBootstrap>(new CSharpBootstrap(HarnessType.Test))
{
    public static CSharpTest Instance { get; } = new CSharpTest();

    public override string RenderContains<T>(GeneratorSpec spec, T[] present, T[] notPresent) =>
        $$"""
          {{spec.Source}}

          public static class Program
          {
              public static int Main()
              {
          {{FormatList(present, x => $"""
                                          if (!{spec.Identifier}.Contains({Bootstrap.Map.ToValueLabel(x)}))
                                              return 0;
                                      """, "\n")}}

          {{FormatList(notPresent, x => $"""
                                             if ({spec.Identifier}.Contains({Bootstrap.Map.ToValueLabel(x)}))
                                                 return 0;
                                         """, "\n")}}

                  return 1;
              }
          }
          """;

    public override string RenderTryLookup<TKey, TValue>(GeneratorSpec spec, TestVector<TKey, TValue> vector) =>
        $$"""
          {{spec.Source}}

          public static class Program
          {
              public static int Main()
              {
          {{FormatList(vector.Keys, x => $"""
                                              if (!{spec.Identifier}.TryLookup({Bootstrap.Map.ToValueLabel(x)}, out _))
                                                  return 0;
                                          """, "\n")}}

                  return 1;
              }
          }
          """;

    public override int Run(string fileId, string source)
    {
        return CompilationHelper.GetDelegate<Func<int>>(source, types => types.First(x => x.Name == "Program"), methods => methods.First(x => x.Name == "Main"), false)();
    }
}