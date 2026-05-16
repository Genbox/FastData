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

          fn GetCharAt(value: &str, offset: i32) -> char {
              let bytes = value.as_bytes();
              if offset >= 0 { bytes[offset as usize] as char } else { bytes[bytes.len().wrapping_add(offset as usize)] as char }
          }
          fn GetCharAtLower(value: &str, offset: i32) -> char {
              let bytes = value.as_bytes();
              let ch = if offset >= 0 { bytes[offset as usize] } else { bytes[bytes.len().wrapping_add(offset as usize)] };
              ToLowerAscii(ch) as char
          }
          fn GetLength(value: &str) -> i32 { value.len() as i32 }

          fn StringAt(fragment: &str, offset: i32, value: &str) -> bool {
              let start = if offset >= 0 { offset as usize } else { value.len().wrapping_add(offset as usize) };
              &value[start..start + fragment.len()] == fragment
          }
          fn StringAtIgnoreCase(fragment: &str, offset: i32, value: &str) -> bool {
              let frag_bytes = fragment.as_bytes();
              let value_bytes = value.as_bytes();
              let start = if offset >= 0 { offset as usize } else { value_bytes.len().wrapping_add(offset as usize) };
              for i in 0..frag_bytes.len() {
                  if ToLowerAscii(value_bytes[start + i]) != ToLowerAscii(frag_bytes[i]) {
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