namespace Genbox.FastData.Generator.CSharp.Benchmarks.Code;

public class UnoptimizedArray(string[] data)
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
}