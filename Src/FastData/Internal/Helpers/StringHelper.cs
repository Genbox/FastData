namespace Genbox.FastData.Internal.Helpers;

internal static class StringHelper
{
    internal static StringComparer GetStringComparer(bool ignoreCase) => ignoreCase switch
    {
        true => StringComparer.OrdinalIgnoreCase,
        false => StringComparer.Ordinal
    };
}