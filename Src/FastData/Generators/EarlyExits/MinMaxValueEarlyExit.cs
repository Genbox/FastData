using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks if a value is within a specified minimum and maximum value.</summary>
/// <typeparam name="T">The type of the value to check.</typeparam>
/// <param name="MinValue">The minimum valid value.</param>
/// <param name="MaxValue">The maximum valid value.</param>
public sealed record MinMaxValueEarlyExit<T>(T MinValue, T MaxValue) : IEarlyExit;