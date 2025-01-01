namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalKeyLength
{
    private static readonly string[]?[] _entries =
    [
        null,
        null,
        null,
        null,
        null,
        ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9"],
        ["item10"]
    ];

    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        string?[]? bucket = _entries[value.Length];

        if (bucket == null)
            return false;

        foreach (string? str in bucket)
        {
            if (str == value)
                return true;
        }

        return false;
    }
}