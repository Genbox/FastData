using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner;

public sealed class RustVectorTests : VectorTestsBase
{
    private static readonly ITestHarness _harness = new RustTestHarness();
    protected override ITestHarness Harness => _harness;
}