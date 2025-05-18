using Genbox.FastData.ArrayHash;
using Genbox.FastData.Configs;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Misc;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal class BruteForceAnalyzer(StringProperties props, BruteForceAnalyzerConfig config, Simulator sim) : IStringHashAnalyzer
{
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

    private readonly IAvalancheGenerator[] _avalanchers =
    [
        new AvalancheIdentity(),
        new AvalancheMultiply(Seeds),
        new AvalancheXorRightShift(12, 36)
    ];

    public bool IsAppropriate(StringProperties props) => true; //TODO: Not appropriate when there is a lot of items

    public IEnumerable<Candidate> GetCandidates()
    {
        BruteForceStringHash spec = new BruteForceStringHash();
        BruteForceGenerator segGen = new BruteForceGenerator();
        ArraySegment[] segments = segGen.Generate(props).ToArray();

        int leftAttempts = config.MaxAttempts;
        int leftReturned = config.MaxReturned;
        foreach (ArraySegment segment in segments)
        {
            spec.Segment = segment;

            // try every mixer
            foreach (IMixerGenerator mixGen in _mixers)
            {
                mixGen.Reset();

                while (mixGen.TryGet(out Mixer mixer))
                {
                    spec.Mixer = mixer;

                    // for each mixer, try every avalanche
                    foreach (IAvalancheGenerator avGen in _avalanchers)
                    {
                        avGen.Reset();

                        while (avGen.TryGet(out Avalanche avalanche))
                        {
                            spec.Avalanche = avalanche;

                            Candidate current = sim.Run(spec, () => FitnessHelper.CalculateFitness(props, spec.Segment, spec.GetExpression()));

                            if (current.Fitness >= config.MinFitness)
                            {
                                yield return current;

                                if (leftReturned-- == 0)
                                    yield break;
                            }

                            if (leftAttempts-- == 0)
                                yield break;
                        }
                    }
                }
            }
        }
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
        public void Reset() { }

        public bool TryGet(out Avalanche op)
        {
            op = h => h;
            return false;
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

    private static readonly ulong[] Seeds =
    [
        0xFF51AFD7ED558CCD, 0xC4CEB9FE1A85EC53, //Murmur
    ];

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