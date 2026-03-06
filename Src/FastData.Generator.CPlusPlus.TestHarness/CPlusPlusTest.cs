using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CPlusPlus.TestHarness;

public sealed class CPlusPlusTest(DockerManager manager) : TestBase<CPlusPlusBootstrap>(new CPlusPlusBootstrap(HarnessType.Test), manager)
{
    protected override string RenderContains<TKey>(string source, TKey[] present, TKey[] notPresent) =>
        $$"""
          #include <string>
          #include <iostream>

          {{source}}

          int main()
          {
          {{FormatList(present, x => $"""
                                          if (!fastdata::contains({Bootstrap.Map.ToValueLabel(x)}))
                                              return 0;
                                      """, "\n")}}

          {{FormatList(notPresent, x => $"""
                                             if (fastdata::contains({Bootstrap.Map.ToValueLabel(x)}))
                                                 return 0;
                                         """, "\n")}}

              return 1;
          }
          """;

    protected override string RenderTryLookup<TKey, TValue>(string source, TKey[] present, TValue[] presentValues, TKey[] notPresent) =>
        $$"""
          #include <string>
          #include <iostream>

          {{source}}

          int main()
          {
              const {{Bootstrap.Map.GetTypeName(presentValues[0].GetType())}}* res;

          {{FormatList(present, x => $"""
                                          if (!fastdata::try_lookup({Bootstrap.Map.ToValueLabel(x)}, res))
                                              return 0;
                                      """, "\n")}}

          {{FormatList(notPresent, x => $"""
                                             if (fastdata::try_lookup({Bootstrap.Map.ToValueLabel(x)}, res))
                                                 return 0;
                                         """, "\n")}}

              return 1;
          }
          """;
}