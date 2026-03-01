using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.Rust.TestHarness;

public sealed class RustTest : TestBase<RustBootstrap>
{
    private RustTest() : base(new RustBootstrap(HarnessType.Test)) {}

    public static RustTest Instance { get; } = new RustTest();

    public override string RenderContains<T>(GeneratorSpec spec, T[] present, T[] notPresent) =>
        $$"""
          #![allow(non_camel_case_types)]
          {{spec.Source}}

          fn main() {
          {{FormatList(present, x => $$"""
                                           if !{{spec.Identifier}}::contains({{Bootstrap.Map.ToValueLabel(x)}}) {
                                               std::process::exit(0);
                                           }
                                       """, "\n")}}

          {{FormatList(notPresent, x => $$"""
                                              if {{spec.Identifier}}::contains({{Bootstrap.Map.ToValueLabel(x)}}) {
                                                  std::process::exit(0);
                                              }
                                          """, "\n")}}

              std::process::exit(1);
          }
          """;

    public override string RenderTryLookup<TKey, TValue>(GeneratorSpec spec, TestVector<TKey, TValue> vector) =>
        $$"""
          #![allow(non_camel_case_types)]
          {{spec.Source}}

          fn main() {
          {{FormatList(vector.Keys, x => $$"""
                                               if {{spec.Identifier}}::try_lookup({{Bootstrap.Map.ToValueLabel(x)}}).is_none() {
                                                   std::process::exit(0);
                                               }
                                           """, "\n")}}

              std::process::exit(1);
          }
          """;

    public override int Run(string fileId, string source)
    {
        string executable = Bootstrap.Compiler.Compile(fileId, source);
        return ProcessHelper.RunProcess(executable).ExitCode;
    }
}