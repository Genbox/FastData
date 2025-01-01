namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalBinarySearch
{
    private static readonly string[] _entries = ["item1", "item10", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9"];

    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        int lo = 0;
        int hi = 9;
        while (lo <= hi)
        {
            int i = lo + ((hi - lo) >> 1);
            int order = string.CompareOrdinal(_entries[i], value);

            if (order == 0)
                return true;
            if (order < 0)
                lo = i + 1;
            else
                hi = i - 1;
        }

        return ~lo >= 0;
    }
}