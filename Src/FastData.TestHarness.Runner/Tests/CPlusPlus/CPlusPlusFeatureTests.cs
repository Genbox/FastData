using System.Reflection;
using Genbox.FastData.Config;
using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

[Collection("Docker-CPlusPlus")]
public sealed class CPlusPlusFeatureTests(DockerCPlusPlusFixture fixture) : FeatureTestBase
{
    protected override TestBase Harness { get; } = new CPlusPlusTest(fixture.DockerManager);

    [Fact]
    public void StringKeysWithNulBytesAreRejected()
    {
        StringDataConfig config = new StringDataConfig();
        config.StructureTypeOverride = typeof(ArrayStructure<,>);
        config.EarlyExitConfig.Disabled = true;

        string[] keys = ["valid", "nul\0key"];

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => FastDataGenerator.Generate(keys, config, Harness.Generator));
        InvalidOperationException inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("C++ generator does not support string literals that contain NUL bytes.", inner.Message);
    }
}