using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

[Collection("Docker-CSharp")]
public sealed class CSharpFeatureTests(DockerCSharpFixture fixture) : FeatureTestBase
{
    protected override TestBase Harness { get; } = new CSharpTest(fixture.DockerManager);
}