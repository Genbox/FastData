namespace Genbox.FastData.Internal.Extensions;

public static class TypeExtensions
{
    public static string GetFriendlyName(this Type type)
    {
        int idx = type.Name.IndexOf("Structure", StringComparison.Ordinal);
        return type.Name.Substring(0, idx);
    }
}