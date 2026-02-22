using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner;

public sealed class CPlusPlusVectorTests : VectorTestsBase
{
    private static readonly ITestHarness _harness = new CPlusPlusTestHarness();
    protected override ITestHarness Harness => _harness;
}