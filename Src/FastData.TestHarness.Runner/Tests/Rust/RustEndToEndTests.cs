using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.Rust;

[Collection("Docker-Rust")]
public sealed class RustEndToEndTests(DockerRustFixture fixture) : EndToEndTestsBase
{
    private protected override TestBase Harness { get; } = new RustTest(fixture.DockerManager);
}