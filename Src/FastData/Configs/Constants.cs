namespace Genbox.FastData.Configs;

public sealed class Constants<T>(uint itemCount)
{
    public uint ItemCount { get; } = itemCount;

    public T MinValue { get; set; }
    public T MaxValue { get; set; }

    public uint MinStringLength { get; set; }
    public uint MaxStringLength { get; set; }
}