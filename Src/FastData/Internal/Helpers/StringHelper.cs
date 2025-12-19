using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Internal.Helpers;

internal static class StringHelper
{
    [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs")]
    internal static StringComparer GetStringComparer(StringComparison comparison) => comparison switch
    {
        StringComparison.CurrentCulture => StringComparer.CurrentCulture,
        StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
        StringComparison.InvariantCulture => StringComparer.InvariantCulture,
        StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
        StringComparison.Ordinal => StringComparer.Ordinal,
        StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
        _ => throw new InvalidOperationException("Invalid StringComparison: " + comparison)
    };
}