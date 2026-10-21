namespace Genbox.FastData.Ir.Harness.Runner;

/// <summary>The queries a benchmark cycles through.</summary>
internal enum BenchmarkWorkload
{
    /// <summary>Present keys only.</summary>
    Hit,

    /// <summary>Absent keys only.</summary>
    Miss,

    /// <summary>Present keys followed by absent keys.</summary>
    Mixed
}