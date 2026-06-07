using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.BenchmarkHarness.Runner.Catalog;

internal sealed record BenchmarkSelection(string Name, Func<DockerManager, BenchmarkBase> Factory, ITestData[] Data);