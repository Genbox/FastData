namespace Genbox.FastData.Config.Limits;

public interface ILimit;

public interface ILimit<in T> : ILimit
{
    bool IsWithinLimit(T value);
}