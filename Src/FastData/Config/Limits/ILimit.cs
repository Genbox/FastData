using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Config.Limits;

/// <summary>Marks a constraint used when selecting a structure or early exit.</summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "The non-generic marker permits heterogeneous limits to be stored together while the generic interface supplies type-safe evaluation.")]
public interface ILimit;

/// <summary>Defines a constraint over values of a specified type.</summary>
/// <typeparam name="T">The value type evaluated by the constraint.</typeparam>
public interface ILimit<in T> : ILimit
{
    /// <summary>Determines whether a value satisfies the constraint.</summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns><see langword="true" /> when the value satisfies the constraint; otherwise, <see langword="false" />.</returns>
    bool IsWithinLimit(T value);
}