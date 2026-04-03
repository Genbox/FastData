using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

[Collection("Docker-CPlusPlus")]
public sealed class CPlusPlusEndToEndTests(DockerCPlusPlusFixture fixture) : EndToEndTestsBase
{
    protected override TestBase Harness { get; } = new CPlusPlusTest(fixture.DockerManager);
}