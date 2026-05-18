using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CSharp;

[Collection("Docker-CSharp")]
public sealed class CSharpFeatureTests(DockerCSharpFixture fixture) : FeatureTestBase
{
    protected override TestBase Harness { get; } = new CSharpTest(fixture.DockerManager);

    protected override ICodeGenerator GetIgnoreCaseGenerator(bool ignoreCase)
    {
        if (!ignoreCase)
            return Harness.Generator;

        CSharpCodeGeneratorConfig config = new CSharpCodeGeneratorConfig("FastData");
        config.ConditionalBranchType = BranchType.If;
        return new CSharpCodeGenerator(config);
    }
}