using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Tests.Code;

/// <summary>Delegates calls to random to someplace else. Used in tests.</summary>
internal sealed class DelegatedRandom(Func<int> getInt, Func<double> getDouble) : IRandom
{
    public int Next() => getInt();
    public int Next(int maxValue) => getInt();
    public int Next(int minValue, int maxValue) => getInt();
    public double NextDouble() => getDouble();
}