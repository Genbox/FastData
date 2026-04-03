using System.Diagnostics;
using System.Text;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal;

internal static class HashBenchmark
{
    internal static Candidate GetBestHash(ReadOnlySpan<string> data, StringKeyProperties props, StringAnalyzerConfig cfg, ILoggerFactory factory, GeneratorEncoding encoding, bool includeDefault)
    {
        Simulator sim = new Simulator(data.Length, encoding);

        //Run each of the analyzers
        List<Candidate> candidates = new List<Candidate>(16);

        //We always add the default hash as a candidate
        if (includeDefault)
            candidates.Add(sim.Run(data, DefaultStringHash.GetInstance(encoding)));

        if (cfg.BruteForceAnalyzerConfig != null)
        {
            BruteForceAnalyzer bf = new BruteForceAnalyzer(props, cfg.BruteForceAnalyzerConfig, sim, factory.CreateLogger<BruteForceAnalyzer>());
            if (bf.IsAppropriate())
                candidates.AddRange(bf.GetCandidates(data));
        }

        if (cfg.GeneticAnalyzerConfig != null)
        {
            GeneticAnalyzer ga = new GeneticAnalyzer(props, cfg.GeneticAnalyzerConfig, sim, factory.CreateLogger<GeneticAnalyzer>());
            if (ga.IsAppropriate())
                candidates.AddRange(ga.GetCandidates(data));
        }

        if (cfg.GPerfAnalyzerConfig != null)
        {
            GPerfAnalyzer ha = new GPerfAnalyzer(data.Length, props, cfg.GPerfAnalyzerConfig, sim, factory.CreateLogger<GPerfAnalyzer>());
            if (ha.IsAppropriate())
                candidates.AddRange(ha.GetCandidates(data));
        }

        //Split candidates into perfect and not perfect
        List<Candidate> perfect = new List<Candidate>(candidates.Count);
        List<Candidate> notPerfect = new List<Candidate>(candidates.Count);

        foreach (Candidate candidate in candidates)
        {
            if (candidate.Collisions == 0)
                perfect.Add(candidate);
            else
                notPerfect.Add(candidate);
        }

        //Sort both on fitness
        perfect.Sort(static (a, b) => b.Fitness.CompareTo(a.Fitness));
        notPerfect.Sort(static (a, b) => b.Fitness.CompareTo(a.Fitness));

        string test = new string('a', props.LengthData.MaxCharLength);
        Func<string, byte[]> getBytes = StringHelper.GetBytesFunc(encoding);
        byte[] testBytes = getBytes(test);

        //We start with the perfect results (if any)
        if (perfect.Count > 0)
        {
            foreach (Candidate candidate in perfect)
                Benchmark(testBytes, cfg.BenchmarkIterations, candidate);

            //Sort by time
            perfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));

            //Take the first non-perfect candidate (the highest fitness) and benchmark it too
            if (notPerfect.Count > 0)
            {
                Candidate np = notPerfect[0];
                Benchmark(testBytes, cfg.BenchmarkIterations, np);

                //If the perfect is faster, we use that one.
                Candidate p = perfect[0];

                if (p.Time <= np.Time)
                    return p;

                //If the not-perfect is faster, it has to be so by 25% before we pick it over a perfect hash.
                //E.g. we still want the perfect, even if it is 25% slower

                double threshold = p.Time + (p.Time * cfg.PerfectHashThreshold);

                if (np.Time < threshold)
                    return np;

                return p;
            }

            return perfect[0];
        }

        //If there are no perfect candidates, we benchmark all the not-perfect candidates
        foreach (Candidate candidate in notPerfect)
            Benchmark(testBytes, cfg.BenchmarkIterations, candidate);

        notPerfect.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return notPerfect[0];
    }

    private static void Benchmark(byte[] data, int iterations, Candidate candidate)
    {
        //The candidate has already been benchmarked. Do nothing.
        if (candidate.Time >= double.Epsilon)
            return;

        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        //Warmup
        for (int i = 0; i < iterations; i++)
            func(data, data.Length);

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
            func(data, data.Length);

        sw.Stop();

        candidate.Time = sw.ElapsedTicks / (double)iterations;
    }
}