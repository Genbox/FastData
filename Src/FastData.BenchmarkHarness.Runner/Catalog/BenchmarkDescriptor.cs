using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.BenchmarkHarness.Runner.Catalog;

internal sealed record BenchmarkDescriptor(string Name, Func<DockerManager, BenchmarkBase> Factory);