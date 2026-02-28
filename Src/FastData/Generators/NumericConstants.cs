namespace Genbox.FastData.Generators;

/// <summary>Represents numeric constants used by code generators in the FastData library.</summary>
/// <typeparam name="TKey">The type of value for min/max value constants.</typeparam>
public sealed class NumericConstants<TKey>
{
    /// <summary>Gets or sets the minimum value for the data type.</summary>
    public TKey MinValue { get; set; } = default!;

    /// <summary>Gets or sets the maximum value for the data type.</summary>
    public TKey MaxValue { get; set; } = default!;
}