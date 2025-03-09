namespace Genbox.FastData.Models;

public class SwitchHashContext(object[] data, KeyValuePair<uint, object>[] hashCodes) : DefaultContext(data)
{
    public KeyValuePair<uint, object>[] HashCodes { get; } = hashCodes;
}