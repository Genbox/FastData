using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Benchmarks.Code;

public class UnoptimizedHashSet(string[] data) : IFastSet
{
    private readonly HashSet<string> _data = new HashSet<string>(data, StringComparer.Ordinal);
    public bool Contains(string value) => _data.Contains(value);
    public int Length => _data.Count;
}