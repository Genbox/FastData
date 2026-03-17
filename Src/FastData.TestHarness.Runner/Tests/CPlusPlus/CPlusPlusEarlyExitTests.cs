using Genbox.FastData.Generator;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

[Collection("Docker-CPlusPlus")]
public sealed class CPlusPlusEarlyExitTests(DockerCPlusPlusFixture fixture) : EarlyExitTestBase
{
    private static readonly CPlusPlusBootstrap Bootstrap = new CPlusPlusBootstrap(HarnessType.Test);

    protected override string HarnessName => Harness.Name;
    protected override TestBase Harness { get; } = new CPlusPlusTest(fixture.DockerManager);
    protected override ExpressionCompiler CreateCompiler() => Bootstrap.CreateExpressionCompiler();

    protected override string RenderProgram(string expression, Type keyType)
    {
        string keyInit = GetKeyInit(keyType);

        return $$"""
                 #include <cstdint>
                 #include <string_view>

                 int main()
                 {
                 {{keyInit}}
                     bool result = {{expression}};
                     (void)result;
                     return 1;
                 }
                 """;
    }

    private static string GetKeyInit(Type keyType)
    {
        if (keyType == typeof(string))
            return "    std::string_view key = \"prefixsuf\";";

        if (keyType == typeof(int))
            return "    int32_t key = 15;";

        if (keyType == typeof(ulong))
            return "    uint64_t key = 65280ull;";

        throw new InvalidOperationException("Unsupported early-exit key type: " + keyType.FullName);
    }
}