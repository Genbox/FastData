namespace Genbox.FastData.Config.Limits;

/// <summary>Constrains a value density to an inclusive range.</summary>
/// <param name="MinDensity">The inclusive minimum density.</param>
/// <param name="MaxDensity">The inclusive maximum density.</param>
public record ValueDensityMinMaxLimit(float MinDensity, float MaxDensity) : ILimit<float>
{
    /// <inheritdoc />
    public bool IsWithinLimit(float value) => value >= MinDensity && value <= MaxDensity;
}