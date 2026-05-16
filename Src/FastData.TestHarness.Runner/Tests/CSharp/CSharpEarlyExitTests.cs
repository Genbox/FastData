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
              private static char GetCharAt(string str, int offset) => offset >= 0 ? str[offset] : str[str.Length + offset];
              private static char GetCharAtLower(string str, int offset) => char.ToLowerInvariant(offset >= 0 ? str[offset] : str[str.Length + offset]);
              private static int GetLength(string str) => str.Length;
              private static bool StartsWith(string prefix, string str) => str.StartsWith(prefix, StringComparison.Ordinal);
              private static bool StartsWithIgnoreCase(string prefix, string str) => str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
              private static bool EndsWith(string prefix, string str) => str.EndsWith(prefix, StringComparison.Ordinal);
              private static bool EndsWithIgnoreCase(string prefix, string str) => str.EndsWith(prefix, StringComparison.OrdinalIgnoreCase);

              public static int Main()
              {
                  {{Bootstrap.Map.GetTypeName(value.GetType())}} inputKey = {{Bootstrap.Map.ToValueLabel(value)}};
                  return {{expression}} ? 1 : 0;
              }
          }
          """;
}