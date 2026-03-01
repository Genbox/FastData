using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

public sealed class CPlusPlusFeatureTests : FeatureTestBase
{
    protected override TestBase Harness => CPlusPlusTest.Instance;
}