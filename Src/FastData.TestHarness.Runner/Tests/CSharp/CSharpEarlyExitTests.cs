using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

public sealed class CSharpEarlyExitTests : EarlyExitTestBase
{
    private static readonly CSharpBootstrap Bootstrap = new CSharpBootstrap(HarnessType.Test);

    protected override string HarnessName => Bootstrap.Name;
    protected override ExpressionCompiler CreateCompiler() => Bootstrap.CreateExpressionCompiler();
}