namespace Genbox.FastData.Configs;

public class Constants(uint itemCount)
{
    public uint ItemCount { get; set; } = itemCount;
    public object MinValue { get; set; } // When DataType is string, this contains the length of the shortest string
    public object MaxValue { get; set; } // When DataType is string, this contains the length of the longest string
}