namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalUniqueKeyLengthSwitch
{
    public static bool Contains(string value)
    {
        if (value.Length is < 1 or > 10)
            return false;

        return value.Length switch
        {
            1 => value == "a",
            2 => value == "aa",
            3 => value == "aaa",
            4 => value == "aaaa",
            5 => value == "aaaaa",
            6 => value == "aaaaaa",
            7 => value == "aaaaaaa",
            8 => value == "aaaaaaaa",
            9 => value == "aaaaaaaaa",
            10 => value == "aaaaaaaaaa",
            _ => false
        };
    }
}