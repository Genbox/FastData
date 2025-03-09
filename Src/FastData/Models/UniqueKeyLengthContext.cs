namespace Genbox.FastData.Models;

public class UniqueKeyLengthContext(object[] data, string?[] lengths, int lowerBound) : DefaultContext(data)
{
    public string?[] Lengths { get; } = lengths;
    public int LowerBound { get; } = lowerBound;
}