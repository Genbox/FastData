using Genbox.FastData.Generator;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.Rust;

[Collection("Docker-Rust")]
public sealed class RustEarlyExitTests(DockerRustFixture fixture) : EarlyExitTestBase
{
    private static readonly RustBootstrap Bootstrap = new RustBootstrap(HarnessType.Test);

    protected override string HarnessName => Harness.Name;
    protected override TestBase Harness { get; } = new RustTest(fixture.DockerManager);
    protected override ExpressionCompiler CreateCompiler() => Bootstrap.CreateExpressionCompiler();

    protected override string RenderProgram(string expression, Type keyType)
    {
        string keyInit = GetKeyInit(keyType);
        string keyTypeName = GetKeyTypeName(keyType);

        return $$"""
                 fn early_exit(key: {{keyTypeName}}) -> bool {
                     {{expression}}
                 }

                 fn main() {
                 {{keyInit}}
                     let _ = early_exit(key);
                     std::process::exit(1);
                 }
                 """;
    }

    private static string GetKeyInit(Type keyType)
    {
        if (keyType == typeof(string))
            return "    let key: &str = \"prefixsuf\";";

        if (keyType == typeof(int))
            return "    let key: i32 = 15;";

        if (keyType == typeof(ulong))
            return "    let key: u64 = 65280;";

        throw new InvalidOperationException("Unsupported early-exit key type: " + keyType.FullName);
    }

    private static string GetKeyTypeName(Type keyType)
    {
        if (keyType == typeof(string))
            return "&str";

        if (keyType == typeof(int))
            return "i32";

        if (keyType == typeof(ulong))
            return "u64";

        throw new InvalidOperationException("Unsupported early-exit key type: " + keyType.FullName);
    }
}