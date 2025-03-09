using Genbox.FastData.Generator.CSharp.Abstracts;

namespace Genbox.FastData.Benchmarks.Code;

public class UnoptimizedHashSet(string[] data) : IFastSet<string>
{
    private readonly HashSet<string> _data = new HashSet<string>(data, StringComparer.Ordinal);
    public bool Contains(string value) => _data.Contains(value);
    public int Length => _data.Count;
}