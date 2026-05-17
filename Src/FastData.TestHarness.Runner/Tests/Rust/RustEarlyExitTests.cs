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

          fn ToAsciiLower(value: u32) -> u32 {
              let candidate = value | 0x20;
              if candidate.wrapping_sub(b'a' as u32) <= (b'z' - b'a') as u32 {
                  candidate
              } else {
                  value
              }
          }

          fn UnitAt(value: &str, offset: i32) -> u32 {
              let bytes = value.as_bytes();
              let index = if offset >= 0 { offset as usize } else { bytes.len().wrapping_add(offset as usize) };
              bytes[index] as u32
          }
          fn UnitAtAsciiLower(value: &str, offset: i32) -> u32 {
              ToAsciiLower(UnitAt(value, offset))
          }
          fn Length(value: &str) -> i32 { value.len() as i32 }

          fn EqualsAt(value: &str, offset: i32, fragment: &str) -> bool {
              let start = if offset >= 0 { offset as usize } else { value.len().wrapping_add(offset as usize) };
              &value[start..start + fragment.len()] == fragment
          }
          fn EqualsAtAsciiLower(value: &str, offset: i32, fragment: &str) -> bool {
              let frag_bytes = fragment.as_bytes();
              let value_bytes = value.as_bytes();
              let start = if offset >= 0 { offset as usize } else { value_bytes.len().wrapping_add(offset as usize) };
              for i in 0..frag_bytes.len() {
                  if ToAsciiLower(value_bytes[start + i] as u32) != ToAsciiLower(frag_bytes[i] as u32) {
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