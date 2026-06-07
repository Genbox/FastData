namespace Genbox.FastData.Config.Limits;

public record ValueMinMaxLimit<T>(T MinValue, T MaxValue) : ILimit<T>
{
    public bool IsWithinLimit(T value) => Comparer<T>.Default.Compare(value, MinValue) >= 0 && Comparer<T>.Default.Compare(value, MaxValue) <= 0;
}