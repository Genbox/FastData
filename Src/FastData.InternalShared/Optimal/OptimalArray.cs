namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalArray
{
    private static readonly string[] _entries =
    [
        "item1",
        "item2",
        "item3",
        "item4",
        "item5",
        "item6",
        "item7",
        "item8",
        "item9",
        "item10"
    ];

    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        for (int i = 0; i < 10; i++)
        {
            if (_entries[i].Equals(value, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}