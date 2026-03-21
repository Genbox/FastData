using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.Rust;

[Collection("Docker-Rust")]
public sealed class RustEarlyExitTests(DockerRustFixture fixture) : EarlyExitTestBase(new RustTest(fixture.DockerManager), Bootstrap.CreateExpressionCompiler())
{
    private static readonly RustBootstrap Bootstrap = new RustBootstrap(HarnessType.Test);

    protected override string RenderProgram(string expression, object value) =>
        $$"""
          #![allow(non_snake_case)]

          fn ToLowerAscii(value: u8) -> u8 {
              if value >= b'A' && value <= b'Z' {
                  value + 32
              } else {
                  value
              }
          }

          fn GetFirstChar(value: &str) -> u8 { value.as_bytes()[0] }
          fn GetFirstCharLower(value: &str) -> u8 { ToLowerAscii(GetFirstChar(value)) }
          fn GetLastChar(value: &str) -> u8 {
              let bytes = value.as_bytes();
              bytes[bytes.len() - 1]
          }
          fn GetLastCharLower(value: &str) -> u8 { ToLowerAscii(GetLastChar(value)) }
          fn GetLength(value: &str) -> u32 { value.len() as u32 }

          fn StartsWith(prefix: &str, value: &str) -> bool { value.starts_with(prefix) }
          fn StartsWithIgnoreCase(prefix: &str, value: &str) -> bool {
              let prefix_bytes = prefix.as_bytes();
              let value_bytes = value.as_bytes();

              if value_bytes.len() < prefix_bytes.len() {
                  return false;
              }

              for i in 0..prefix_bytes.len() {
                  if ToLowerAscii(value_bytes[i]) != ToLowerAscii(prefix_bytes[i]) {
                      return false;
                  }
              }

              true
          }

          fn EndsWith(suffix: &str, value: &str) -> bool { value.ends_with(suffix) }
          fn EndsWithIgnoreCase(suffix: &str, value: &str) -> bool {
              let suffix_bytes = suffix.as_bytes();
              let value_bytes = value.as_bytes();

              if value_bytes.len() < suffix_bytes.len() {
                  return false;
              }

              let start = value_bytes.len() - suffix_bytes.len();
              for i in 0..suffix_bytes.len() {
                  if ToLowerAscii(value_bytes[start + i]) != ToLowerAscii(suffix_bytes[i]) {
                      return false;
                  }
              }

              true
          }

          {{Bootstrap.Wrap($"""
                                    let inputKey = {Bootstrap.Map.ToValueLabel(value)};
                                    std::process::exit(({expression}) as i32);
                            """)}}
          """;
}