using System.Diagnostics;
using Genbox.FastData.Internal.Analysis.Genetic;

namespace Genbox.FastData.Internal.Analysis;

internal static class AnalysisHelper
{
    internal static void RunSimulation<T>(string[] data, CommonSettings settings, ref Candidate<T> candidate) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        Func<string, uint> hashFunc = candidate.Spec.GetFunction();

        int capacity = (int)(data.Length * settings.CapacityFactor);

        long ticks = Stopwatch.GetTimestamp();
        for (int i = 0; i < 1000; i++)
            hashFunc(data[0]);
        ticks = Stopwatch.GetTimestamp() - ticks;

        (int occupied, double minVariance, double maxVariance) = HashSetEmulator.Run(data, capacity, hashFunc);

        double normOccu = (occupied / (double)capacity) * settings.FillWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * settings.TimeWeight;

        candidate.Fitness = (normOccu + normTime) / 2;
        candidate.Metadata = [("Time/norm", ticks + "/" + normTime.ToString("N2")), ("Occupied/norm", occupied + "/" + normOccu.ToString("N2")), ("MinVariance", minVariance), ("MaxVariance", maxVariance)];
    }
}