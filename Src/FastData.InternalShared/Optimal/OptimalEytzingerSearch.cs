namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalEytzingerSearch
{
    private static readonly string[] _entries =
    [
        "item6",
        "item3",
        "item8",
        "item10",
        "item5",
        "item7",
        "item9",
        "item1",
        "item2",
        "item4"
    ];

    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        int i = 0;
        while (i < _entries.Length)
        {
            int comparison = string.CompareOrdinal(_entries[i], value);

            if (comparison == 0)
                return true;

            if (comparison < 0)
                i = (2 * i) + 2;
            else
                i = (2 * i) + 1;
        }

        return false;
    }
}