namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalUniqueKeyLength
{
    private static readonly string[] _entries =
    [
        "a",
        "aa",
        "aaa",
        "aaaa",
        "aaaaa",
        "aaaaaa",
        "aaaaaaa",
        "aaaaaaaa",
        "aaaaaaaaa",
        "aaaaaaaaaa"
    ];

    public static bool Contains(string value)
    {
        if (value.Length is < 1 or > 10)
            return false;

        return _entries[value.Length - 1] == value;
    }
}