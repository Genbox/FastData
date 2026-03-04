using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

[Collection("Docker-CSharp")]
public sealed class CSharpVectorTests(DockerCSharpFixture fixture) : VectorTestsBase
{
    protected override TestBase Harness { get; } = new CSharpTest(fixture.DockerManager);
}