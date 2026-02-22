using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner;

public sealed class CSharpVectorTests : VectorTestsBase
{
    private static readonly ITestHarness _harness = new CSharpTestHarness();
    protected override ITestHarness Harness => _harness;
}