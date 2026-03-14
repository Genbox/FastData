namespace Genbox.FastData.Config.Limits;

public record ValueMinMaxLimit<T>(T MinValue, T MaxValue) : ILimit<T>
{
    public bool IsWithinLimit(T value) => true;
}