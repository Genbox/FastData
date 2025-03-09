namespace Genbox.FastData.Models;

public class KeyLengthContext(object[] data, List<string>?[] lengths) : DefaultContext(data)
{
    public List<string>?[] Lengths { get; } = lengths;
}