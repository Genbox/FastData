using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusTest : TestBase<CPlusPlusBootstrap>
{
    private CPlusPlusTest() : base(new CPlusPlusBootstrap(HarnessType.Test)) {}

    public static CPlusPlusTest Instance { get; } = new CPlusPlusTest();

    public override string RenderContains<T>(GeneratorSpec spec, T[] present, T[] notPresent) =>
        $$"""
          #include <string>
          #include <iostream>

          {{spec.Source}}

          int main()
          {
          {{FormatList(present, x => $"""
                                          if (!{spec.Identifier}::contains({Bootstrap.Map.ToValueLabel(x)}))
                                              return 0;
                                      """, "\n")}}

          {{FormatList(notPresent, x => $"""
                                             if ({spec.Identifier}::contains({Bootstrap.Map.ToValueLabel(x)}))
                                                 return 0;
                                         """, "\n")}}

              return 1;
          }
          """;

    public override string RenderTryLookup<TKey, TValue>(GeneratorSpec spec, TestVector<TKey, TValue> vector) =>
        $$"""
          #include <string>
          #include <iostream>

          {{spec.Source}}

          int main()
          {
              const {{Bootstrap.Map.GetTypeName(vector.Values[0].GetType())}}* res;
          {{FormatList(vector.Keys, x => $"""
                                              if (!{spec.Identifier}::try_lookup({Bootstrap.Map.ToValueLabel(x)}, res))
                                                  return 0;
                                          """, "\n")}}

              return 1;
          }
          """;

    public override int Run(string fileId, string source)
    {
        string executable = Bootstrap.Compiler.Compile(fileId, source);
        return ProcessHelper.RunProcess(executable).ExitCode;
    }
}