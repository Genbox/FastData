namespace Genbox.FastData.Ir.Harness.Runner;

internal sealed record GeneratedHarness(string Source, int QueryCount, int HitsPerCycle, bool Exact);