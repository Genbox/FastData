namespace Genbox.FastData.InternalShared.Harness;

public readonly record struct BenchmarkSuite(string EntryFilename, string EntrySource, IReadOnlyList<BenchmarkFile> AdditionalFiles);