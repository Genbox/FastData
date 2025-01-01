using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Benchmarks.Code;

public class UnoptimizedArray(string[] data) : IFastSet
{
    public bool Contains(string value) => data.Contains(value, StringComparer.Ordinal);
    public int Length => data.Length;
}