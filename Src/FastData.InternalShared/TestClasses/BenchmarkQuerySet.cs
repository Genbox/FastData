namespace Genbox.FastData.InternalShared.TestClasses;

internal readonly record struct BenchmarkQuerySet(string[] Keys, int ExpectedFoundCount, bool ValidateFoundCount);