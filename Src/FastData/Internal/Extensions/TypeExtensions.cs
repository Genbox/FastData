namespace Genbox.FastData.Internal.Extensions;

public static class TypeExtensions
{
    public static string GetCleanName(this Type type)
    {
        string name = type.Name;
        int tick = name.IndexOf('`');
        return tick < 0 ? name : name.Substring(0, tick);
    }
}