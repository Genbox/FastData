using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Misc;

internal sealed class StructureConfig<T>(DataProperties<T> dataProperties, StringComparison comparison = StringComparison.Ordinal)
{
    public DataProperties<T> DataProperties { get; } = dataProperties;

    internal StringComparer GetStringComparer() => comparison switch
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