namespace Genbox.FastData.Generator.CSharp.Benchmarks.Code;

public class UnoptimizedHashSet(string[] data)
{
    private readonly HashSet<string> _data = new HashSet<string>(data, StringComparer.Ordinal);
    public bool Contains(string value) => _data.Contains(value);
}