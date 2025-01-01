namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalConditional
{
    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        return value is "item1" or "item2" or "item3" or "item4" or "item5" or "item6" or "item7" or "item8" or "item9" or "item10";
    }
}