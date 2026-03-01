using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.Rust;

public sealed class RustFeatureTests : FeatureTestBase
{
    protected override TestBase Harness => RustTest.Instance;
}