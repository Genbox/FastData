namespace Genbox.FastData.InternalShared.TestClasses;

public readonly record struct BenchmarkQuerySet(string[] Keys, int ExpectedFoundCount, bool ValidateFoundCount);