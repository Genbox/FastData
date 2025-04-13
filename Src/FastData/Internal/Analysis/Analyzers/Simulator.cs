using System.Diagnostics;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

public delegate bool EqualFunc(string a, string b);
public delegate uint HashFunc(string a);

internal class Simulator(SimulatorConfig config, object[] data, Func<object[], uint, HashFunc, EqualFunc, double[]> emulator)
{
    public void Run<T>(ref Candidate<T> cand) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        HashFunc hashFunc = cand.Spec.GetHashFunction();
        EqualFunc equalFunc = cand.Spec.GetEqualFunction();
        uint capacity = (uint)(data.Length * config.CapacityFactor);

        long ticks = Stopwatch.GetTimestamp();
        double[] results = emulator(data, capacity, hashFunc, equalFunc);
        ticks = Stopwatch.GetTimestamp() - ticks;

        double normEmu = results.Average() * config.EmulationWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * config.TimeWeight;

        //TODO: When weights are 0, we don't want to include their values, just so we always get [0..1]
        cand.Fitness = (normEmu + normTime) / 2;

        cand.Metadata["EmulationNormalized"] = normEmu;
        cand.Metadata["Time"] = ticks;
        cand.Metadata["TimeNormalized"] = normTime;
    }
}