using System.Globalization;
using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.InternalShared.Misc;

namespace Genbox.FastData.Ir.Harness.Runner;

internal static class BenchmarkMeasurement
{
    private const int MaxPilots = 20;

    public static async Task<(long Invocations, BenchmarkSample[] Samples)> RunAsync(DockerHarness docker, HarnessBackend backend, string directory, string id, GeneratedHarness generated, HarnessOptions options, CancellationToken cancellationToken)
    {
        // Invocation counts are whole query cycles, so every sample has an exact expected hit count.
        long invocations = RoundToQueryCycle(options.Invocations ?? generated.QueryCount);

        if (options.Invocations == null)
            invocations = await CalibrateAsync(invocations, options.TargetMs * 1_000_000.0);

        return (invocations, await MeasureAsync(invocations, options.Warmups, options.Samples));

        // Grows the invocation count by 2-10x per pilot run until one sample reaches the target duration.
        async Task<long> CalibrateAsync(long count, double targetNs)
        {
            for (int pilot = 0; pilot < MaxPilots; pilot++)
            {
                BenchmarkSample sample = (await MeasureAsync(count, 1, 1))[0];
                if (sample.ElapsedNs >= targetNs)
                    return count;

                double scale = Math.Clamp(targetNs / Math.Max(1, sample.ElapsedNs), 2, 10);
                count = RoundToQueryCycle(checked((long)Math.Ceiling(count * scale)));
            }

            throw new InvalidOperationException("Benchmark calibration did not reach the target duration.");
        }

        long RoundToQueryCycle(long value) => checked((((value + generated.QueryCount) - 1) / generated.QueryCount) * generated.QueryCount);

        async Task<BenchmarkSample[]> MeasureAsync(long count, int warmups, int samples)
        {
            string arguments = string.Create(CultureInfo.InvariantCulture, $"{count} {warmups} {samples}");
            ProcessResult result = await docker.RunAsync(backend, directory, id, arguments, cancellationToken);
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Benchmark failed ({result.ExitCode}): {result.StandardError}");

            return ParseSamples(result.StandardOutput, count, samples, generated);
        }
    }

    private static BenchmarkSample[] ParseSamples(string output, long invocations, int sampleCount, GeneratedHarness generated)
    {
        string[] lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != sampleCount * 2)
            throw new InvalidOperationException("Unexpected benchmark output: " + output);

        long expectedHits = checked((invocations / generated.QueryCount) * generated.HitsPerCycle);
        BenchmarkSample[] samples = new BenchmarkSample[sampleCount];

        for (int i = 0; i < samples.Length; i++)
        {
            BenchmarkSample sample = ParseSample(lines[i * 2], lines[(i * 2) + 1]);

            // Approximate structures may report false positives, so only exact ones must match the expected count.
            bool wrongHitCount = sample.Found < expectedHits || sample.Found > invocations || (generated.Exact && sample.Found != expectedHits);
            if (wrongHitCount)
                throw new InvalidOperationException($"Wrong benchmark hit count: {sample.Found}, expected {expectedHits}.");

            samples[i] = sample;
        }

        return samples;
    }

    private static BenchmarkSample ParseSample(string overheadLine, string sampleLine)
    {
        string[] overhead = overheadLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] measurement = sampleLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (overhead.Length != 2 || overhead[0] != "overhead" || measurement.Length != 3 || measurement[0] != "sample")
            throw new InvalidOperationException("Invalid measurement record.");

        double overheadNs = double.Parse(overhead[1], CultureInfo.InvariantCulture);
        double elapsedNs = double.Parse(measurement[1], CultureInfo.InvariantCulture);
        long found = long.Parse(measurement[2], CultureInfo.InvariantCulture);
        if (!double.IsFinite(elapsedNs) || !double.IsFinite(overheadNs) || elapsedNs < 0 || overheadNs < 0)
            throw new InvalidOperationException("Invalid timing result.");

        return new BenchmarkSample(overheadNs, elapsedNs, found);
    }
}