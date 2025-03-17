using Genbox.FastData.Generator.CSharp.Abstracts;

namespace Genbox.FastData.Generator.CSharp.Benchmarks.Code;

public class UnoptimizedArray(string[] data) : IFastSet<string>
{
    public bool Contains(string value)
    {
        foreach (string s in data)
        {
            if (string.Equals(s, value, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public int Length => data.Length;
}