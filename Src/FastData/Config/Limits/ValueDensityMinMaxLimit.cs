namespace Genbox.FastData.Config.Limits;

public record ValueDensityMinMaxLimit(float MinDensity, float MaxDensity) : ILimit<float>
{
    public bool IsWithinLimit(float value) => value >= MinDensity && value <= MaxDensity;
}