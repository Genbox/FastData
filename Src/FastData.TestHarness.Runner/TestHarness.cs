using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.CSharp.TestHarness;
using Genbox.FastData.Generator.Rust.TestHarness;
using Genbox.FastData.InternalShared.TestHarness;

namespace Genbox.FastData.TestHarness.Runner;

internal static class TestHarness
{
    internal static readonly ITestHarness[] All =
    [
        new CSharpTestHarness(),
        new CPlusPlusTestHarness(),
        new RustTestHarness()
    ];
}