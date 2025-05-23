namespace Genbox.FastData.Configs;

public class Constants<T>(uint itemCount)
{
    public uint ItemCount { get; set; } = itemCount;

    public T MinValue { get; set; }
    public T MaxValue { get; set; }

    public uint MinStringLength { get; set; }
    public uint MaxStringLength { get; set; }
}