using Genbox.FastData.Helpers;

namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalSwitchHashCode
{
    public static bool Contains(string value)
    {
        return unchecked((uint)HashHelper.Hash(value)) switch
        {
            1547457659 => value == "item1",
            3756450423 => value == "item2",
            731309710 => value == "item3",
            111896862 => value == "item4",
            120110704 => value == "item5",
            236029066 => value == "item6",
            3119466618 => value == "item7",
            1860819585 => value == "item8",
            968102074 => value == "item9",
            1975224761 => value == "item10",
            _ => false
        };
    }
}