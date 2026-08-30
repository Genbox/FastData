namespace Genbox.FastData.Config.Limits;

/// <summary>Constrains a comparable value to an inclusive range.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="MinValue">The inclusive minimum value.</param>
/// <param name="MaxValue">The inclusive maximum value.</param>
public record ValueMinMaxLimit<T>(T MinValue, T MaxValue) : ILimit<T>
{
    /// <inheritdoc />
    public bool IsWithinLimit(T value) => Comparer<T>.Default.Compare(value, MinValue) >= 0 && Comparer<T>.Default.Compare(value, MaxValue) <= 0;
}