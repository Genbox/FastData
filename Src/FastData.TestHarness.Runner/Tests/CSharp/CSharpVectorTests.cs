using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

public sealed class CSharpVectorTests : VectorTestsBase
{
    protected override TestBase Harness => CSharpTest.Instance;
}