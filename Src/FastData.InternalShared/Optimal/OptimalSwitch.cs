namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalSwitch
{
    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        switch (value)
        {
            case "item1":
            case "item2":
            case "item3":
            case "item4":
            case "item5":
            case "item6":
            case "item7":
            case "item8":
            case "item9":
            case "item10":
                return true;
            default:
                return false;
        }
    }
}