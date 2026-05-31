using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.Rust;

public sealed class RustStringHashTests : StringHashTestBase
{
    private static readonly RustBootstrap Bootstrap = new RustBootstrap(HarnessType.Test);

    protected override string HarnessName => Bootstrap.Name;
    protected override ICodeGenerator Generator => Bootstrap.Generator;
}