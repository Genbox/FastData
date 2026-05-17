using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

[Collection("Docker-CSharp")]
public sealed class CSharpEarlyExitTests(DockerCSharpFixture fixture) : EarlyExitTestBase(new CSharpTest(fixture.DockerManager), Bootstrap.CreateExpressionCompiler())
{
    private static readonly CSharpBootstrap Bootstrap = new CSharpBootstrap(HarnessType.Test);

    protected override string RenderProgram(string expression, object value) =>
        $$"""
          using System;

          public static class Program
          {
              private static uint UnitAt(string str, int offset) => offset >= 0 ? str[offset] : str[str.Length + offset];
              private static uint UnitAtAsciiLower(string str, int offset) { uint unit = UnitAt(str, offset); uint candidate = unit | 0x20u; return candidate - 'a' <= 'z' - 'a' ? candidate : unit; }
              private static int Length(string str) => str.Length;
              private static bool EqualsAt(string str, int offset, string fragment) { int start = offset >= 0 ? offset : str.Length + offset; return string.CompareOrdinal(str, start, fragment, 0, fragment.Length) == 0; }
              private static bool EqualsAtAsciiLower(string str, int offset, string fragment) { int start = offset >= 0 ? offset : str.Length + offset; for (int i = 0; i < fragment.Length; i++) { uint left = str[start + i]; uint right = fragment[i]; uint leftCandidate = left | 0x20u; uint rightCandidate = right | 0x20u; left = leftCandidate - 'a' <= 'z' - 'a' ? leftCandidate : left; right = rightCandidate - 'a' <= 'z' - 'a' ? rightCandidate : right; if (left != right) return false; } return true; }

              public static int Main()
              {
                  {{Bootstrap.Map.GetTypeName(value.GetType())}} inputKey = {{Bootstrap.Map.ToValueLabel(value)}};
                  return {{expression}} ? 1 : 0;
              }
          }
          """;
}