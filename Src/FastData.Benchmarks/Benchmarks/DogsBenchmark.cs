using BenchmarkDotNet.Order;

namespace Genbox.FastData.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark used in Readme
/// </summary>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DogsBenchmark
{
    private readonly string[] _array = ["Labrador", "German Shepherd", "Golden Retriever"];

    [Benchmark(Baseline = true)]
    public bool Array() => _array.Contains("Beagle");

    [Benchmark]
    public bool Generated() => Dogs.Contains("Beagle");

    private static class Dogs
    {
        public static bool Contains(string value)
        {
            if ((49280UL & (1UL << (value.Length - 1) % 64)) == 0)
                return false;

            return value switch
            {
                "Labrador" or "German Shepherd" or "Golden Retriever" => true,
                _ => false
            };
        }
    }
}