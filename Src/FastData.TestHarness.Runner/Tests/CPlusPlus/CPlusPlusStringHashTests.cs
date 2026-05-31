using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

public sealed class CPlusPlusStringHashTests : StringHashTestBase
{
    private static readonly CPlusPlusBootstrap Bootstrap = new CPlusPlusBootstrap(HarnessType.Test);

    protected override string HarnessName => Bootstrap.Name;
    protected override ICodeGenerator Generator => Bootstrap.Generator;
}