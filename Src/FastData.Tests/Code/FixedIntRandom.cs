using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Tests.Code;

/// <summary>Returns a fixed set of values. Used in tests.</summary>
internal sealed class FixedIntRandom(IEnumerable<int> intValues) : IRandom
{
    private readonly Queue<int> _intValues = new Queue<int>(intValues);

    public int Next() => _intValues.Dequeue();
    public int Next(int maxValue) => _intValues.Dequeue();
    public int Next(int minValue, int maxValue) => _intValues.Dequeue();
    public double NextDouble() => throw new NotSupportedException();
}