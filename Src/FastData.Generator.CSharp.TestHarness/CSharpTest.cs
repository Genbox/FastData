using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpTest(DockerManager manager) : TestBase<CSharpBootstrap>(new CSharpBootstrap(HarnessType.Test), manager)
{
    protected override string RenderContains<TKey>(string source, TKey[] present, TKey[] notPresent) =>
        $$"""
          {{source}}

          public static class Program
          {
              public static int Main()
              {
          {{FormatList(present, x => $"""
                                          if (!FastData.Contains({Bootstrap.Map.ToValueLabel(x)}))
                                              return 0;
                                      """, "\n")}}

          {{FormatList(notPresent, x => $"""
                                             if (FastData.Contains({Bootstrap.Map.ToValueLabel(x)}))
                                                 return 0;
                                         """, "\n")}}

                  return 1;
              }
          }
          """;

    protected override string RenderTryLookup<TKey, TValue>(string source, TKey[] present, TValue[] presentValues, TKey[] notPresent) =>
        $$"""
          {{source}}

          public static class Program
          {
              public static int Main()
              {
          {{FormatList(present, x => $"""
                                          if (!FastData.TryLookup({Bootstrap.Map.ToValueLabel(x)}, out _))
                                              return 0;
                                      """, "\n")}}

          {{FormatList(notPresent, x => $"""
                                             if (FastData.TryLookup({Bootstrap.Map.ToValueLabel(x)}, out _))
                                                 return 0;
                                         """, "\n")}}

                  return 1;
              }
          }
          """;
}