using Genbox.FastData.Generator;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

[Collection("Docker-CSharp")]
public sealed class CSharpEarlyExitTests(DockerCSharpFixture fixture) : EarlyExitTestBase
{
    private static readonly CSharpBootstrap Bootstrap = new CSharpBootstrap(HarnessType.Test);

    protected override string HarnessName => Harness.Name;
    protected override TestBase Harness { get; } = new CSharpTest(fixture.DockerManager);
    protected override ExpressionCompiler CreateCompiler() => Bootstrap.CreateExpressionCompiler();

    protected override string RenderProgram(string expression, Type keyType)
    {
        string keyInit = GetKeyInit(keyType);

        return $$"""
                 public static class Program
                 {
                     public static int Main()
                     {
                 {{keyInit}}
                         bool result = {{expression}};
                         _ = result;
                         return 1;
                     }
                 }
                 """;
    }

    private static string GetKeyInit(Type keyType)
    {
        if (keyType == typeof(string))
            return "        string key = \"prefixsuf\";";

        if (keyType == typeof(int))
            return "        int key = 15;";

        if (keyType == typeof(ulong))
            return "        ulong key = 65280ul;";

        throw new InvalidOperationException("Unsupported early-exit key type: " + keyType.FullName);
    }
}