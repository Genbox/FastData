using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Generators.Helpers;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;
using Microsoft.Extensions.Logging;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed partial class BruteForceAnalyzer(StringKeyProperties props, BruteForceAnalyzerConfig config, Simulator sim, ILogger<BruteForceAnalyzer> logger, bool ignoreCase = false) : IStringHashAnalyzer
{
    private static readonly ulong[] Seeds =
    [
        0xFF51AFD7ED558CCD, 0xC4CEB9FE1A85EC53 //Murmur
    ];

    private readonly IAvalancheGenerator[] _avalanchers =
    [
        new AvalancheIdentity(),
        new AvalancheMultiply(Seeds),
        new AvalancheXorRightShift(12, 36)
    ];

    // This brute-forces all combinations of string segments with all possible mixer and avalanche functions.
    // Its initial state is the smallest/fastest in the hope we can reach an optimal state fast.
    // It stops if it reaches the maximum fitness.

    private readonly IMixerGenerator[] _mixers =
    [
        new MixerIdentity(),
        new MixerAdd(),
        new MixerSubtract(),
        new MixerXor(),
        new MixerMultiply(),
        new MixerRotateLeft(12, 36),
        new MixerRotateRight(12, 36),
        new MixerXorShift(12, 36),
        new MixerSquare()
    ];

    public bool IsAppropriate() => true;

    public IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data)
    {
        MinHeap<Candidate> heap = new MinHeap<Candidate>(config.MaxReturned);
        BruteForceGenerator segGen = new BruteForceGenerator(8);
        ArraySegment[] segments = SegmentScorer.Order(props, segGen.Generate(props)).ToArray();
        Candidate? bestPerfect = null;

        int attempts = 0;

        // Try each hash shape across every likely segment before increasing hash complexity.
        foreach (IMixerGenerator mixGen in _mixers)
        {
            mixGen.Reset();

            while (mixGen.TryGet(out Mixer mixer))
            {
                if (logger.IsEnabled(LogLevel.Trace))
                    LogMixer(logger, ExpressionHelper.Print(mixer));

                foreach (IAvalancheGenerator avGen in _avalanchers)
                {
                    avGen.Reset();

                    while (avGen.TryGet(out Avalanche avalanche))
                    {
                        if (logger.IsEnabled(LogLevel.Trace))
                            LogAvalanche(logger, ExpressionHelper.Print(avalanche));

                        foreach (ArraySegment segment in segments)
                        {
                            BruteForceStringHash spec = new BruteForceStringHash(segment, mixer, avalanche, sim.UnitSize) { IgnoreCase = ignoreCase };
                            Candidate current = sim.Run(data, spec, () => FitnessHelper.CalculateFitness(props, spec.Segment, spec.GetExpression()));

                            // A perfect candidate can score lower than a simple non-perfect one, so keep it outside the heap too.
                            if (current.Collisions == 0 && (bestPerfect == null || current.Fitness > bestPerfect.Fitness))
                                bestPerfect = current;

                            if (heap.Add(current.Fitness, current) && logger.IsEnabled(LogLevel.Debug))
                                LogBetterCandidate(logger, current.Fitness, current.Collisions, ExpressionHelper.Print(mixer), ExpressionHelper.Print(avalanche));

                            attempts++;

                            if (current.Collisions == 0 || heap.HasMaxFitness || attempts >= config.MaxAttempts)
                                return GetResults(heap, bestPerfect);
                        }
                    }
                }
            }
        }

        return GetResults(heap, bestPerfect);
    }

    private static Candidate[] GetResults(MinHeap<Candidate> heap, Candidate? bestPerfect)
    {
        List<Candidate> results = heap.Items.Select(static x => x.Item2).ToList();

        // Ensure the final selector sees the best perfect candidate even if blended fitness evicted it from the heap.
        if (bestPerfect != null && !results.Exists(candidate => ReferenceEquals(candidate, bestPerfect)))
            results.Add(bestPerfect);

        return results.ToArray();
    }

    private sealed class MixerIdentity : SimpleMixerGen
    {
        protected override Mixer GetOperation() => static (_, r) => r;
    }

    private sealed class MixerAdd : SimpleMixerGen
    {
        protected override Mixer GetOperation() => Add;
    }

    private sealed class MixerSubtract : SimpleMixerGen
    {
        protected override Mixer GetOperation() => Subtract;
    }

    private sealed class MixerMultiply : SimpleMixerGen
    {
        protected override Mixer GetOperation() => Multiply;
    }

    private sealed class MixerXor : SimpleMixerGen
    {
        protected override Mixer GetOperation() => ExclusiveOr;
    }

    private sealed class MixerRotateLeft(int initial, int max) : MixerGen(initial, max)
    {
        protected override Mixer GetOperation(int idx) => (h, r) =>
            ExclusiveOr(Or(LeftShift(h, Constant(idx)), RightShift(h, Constant(64 - idx))), r);
    }

    private sealed class MixerRotateRight(int initial, int max) : MixerGen(initial, max)
    {
        protected override Mixer GetOperation(int idx) => (h, r) =>
            ExclusiveOr(Or(RightShift(h, Constant(idx)), LeftShift(h, Constant(64 - idx))), r);
    }

    private sealed class MixerXorShift(int initial, int max) : MixerGen(initial, max)
    {
        protected override Mixer GetOperation(int idx) => (h, r) =>
            ExclusiveOr(ExclusiveOr(h, RightShift(h, Constant(idx))), r);
    }

    private sealed class MixerSquare : SimpleMixerGen
    {
        // h = (1 | h) + h*h  then xor r
        protected override Mixer GetOperation() => (h, r) =>
            ExclusiveOr(Add(Or(Constant(1UL), h), Multiply(h, h)), r);
    }

    private sealed class AvalancheIdentity : IAvalancheGenerator
    {
        private bool _used;
        public void Reset() => _used = false;

        public bool TryGet(out Avalanche op)
        {
            if (_used)
            {
                op = null!;
                return false;
            }

            _used = true;
            op = static h => h;
            return true;
        }
    }

    private sealed class AvalancheMultiply(ulong[] seeds) : AvalancheGen(0, seeds.Length - 1)
    {
        protected override Avalanche GetOperation(int idx) => h =>
            Multiply(h, Constant(seeds[idx], typeof(ulong)));
    }

    private sealed class AvalancheXorRightShift(int initial, int max) : AvalancheGen(initial, max)
    {
        protected override Avalanche GetOperation(int idx) => h =>
            ExclusiveOr(h, RightShift(h, Constant(idx)));
    }

    private interface IMixerGenerator
    {
        void Reset();
        bool TryGet(out Mixer op);
    }

    private interface IAvalancheGenerator
    {
        void Reset();
        bool TryGet(out Avalanche op);
    }

    private abstract class MixerGen(int initial, int max) : IMixerGenerator
    {
        private int _current = initial;
        public void Reset() => _current = initial;

        public bool TryGet(out Mixer op)
        {
            if (_current > max)
            {
                op = null!;
                return false;
            }

            op = GetOperation(_current++);

            return true;
        }

        protected abstract Mixer GetOperation(int idx);
    }

    private abstract class SimpleMixerGen : IMixerGenerator
    {
        private bool _used;
        public void Reset() => _used = false;

        public bool TryGet(out Mixer op)
        {
            if (_used)
            {
                op = null!;
                return false;
            }

            _used = true;
            op = GetOperation();
            return true;
        }

        protected abstract Mixer GetOperation();
    }

    private abstract class AvalancheGen(int initial, int max) : IAvalancheGenerator
    {
        private int _current = initial;
        public void Reset() => _current = initial;

        public bool TryGet(out Avalanche op)
        {
            if (_current > max)
            {
                op = null!;
                return false;
            }

            op = GetOperation(_current++);

            return true;
        }

        protected abstract Avalanche GetOperation(int idx);
    }
}