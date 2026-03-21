using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustTest(DockerManager manager) : TestBase<RustBootstrap>(new RustBootstrap(HarnessType.Test), manager)
{
    protected override string RenderContains<TKey>(string source, TKey[] present, TKey[] notPresent) =>
        $"""
         #![allow(non_camel_case_types)]

         {source}

         {Bootstrap.Wrap($"""
                                  {FormatList(present, x => $$"""
                                                                  if !fastdata::contains({{Bootstrap.Map.ToValueLabel(x)}}) {
                                                                      std::process::exit(0);
                                                                  }
                                                              """, "\n")}

                                  {FormatList(notPresent, x => $$"""
                                                                     if fastdata::contains({{Bootstrap.Map.ToValueLabel(x)}}) {
                                                                         std::process::exit(0);
                                                                     }
                                                                 """, "\n")}

                                  std::process::exit(1);
                          """)}
         """;

    protected override string RenderTryLookup<TKey, TValue>(string source, TKey[] present, TValue[] presentValues, TKey[] notPresent) =>
        $"""
         #![allow(non_camel_case_types)]

         {source}

         {Bootstrap.Wrap($"""
                                  {FormatList(present, x => $$"""
                                                                  if fastdata::try_lookup({{Bootstrap.Map.ToValueLabel(x)}}).is_none() {
                                                                      std::process::exit(0);
                                                                  }
                                                              """, "\n")}

                                  {FormatList(notPresent, x => $$"""
                                                                     if fastdata::try_lookup({{Bootstrap.Map.ToValueLabel(x)}}).is_some() {
                                                                         std::process::exit(0);
                                                                     }
                                                                 """, "\n")}

                                  std::process::exit(1);
                          """)}
         """;
}