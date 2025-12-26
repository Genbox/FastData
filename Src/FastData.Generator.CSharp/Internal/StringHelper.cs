namespace Genbox.FastData.Generator.CSharp.Internal;

internal static class StringHelper
{
    internal static string GetStringComparer(bool ignoreCase) => ignoreCase switch
    {
        true => nameof(StringComparer.OrdinalIgnoreCase),
        false => nameof(StringComparer.Ordinal),
    };
}