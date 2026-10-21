using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class GeneticSelectionBenchmarks
{
    private ISelection _boltzmann = null!;
    private ISelection _elite = null!;
    private List<int> _parents = null!;
    private StaticArray<Entity> _population = null!;
    private ISelection _random = null!;
    private ISelection _rank = null!;
    private ISelection _roulette = null!;
    private ISelection _sus = null!;
    private ISelection _tournament = null!;
    private ISelection _uniqueRandom = null!;

    [Params(32, 256, 4096)]
    public int PopulationSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _population = new StaticArray<Entity>(PopulationSize);
        _parents = new List<int>(PopulationSize);
        Random rand = new Random(42);

        for (int i = 0; i < PopulationSize; i++)
            _population.Add(new Entity([]) { Fitness = rand.NextDouble() });

        _boltzmann = new BoltzmannSelection(10, new DefaultRandom(42));
        _elite = new EliteSelection(0.5);
        _random = new RandomSelection(false, new DefaultRandom(42));
        _uniqueRandom = new RandomSelection(true, new DefaultRandom(42));
        _rank = new RankSelection(new DefaultRandom(42));
        _roulette = new RouletteWheelSelection(new DefaultRandom(42));
        _sus = new StochasticUniversalSamplingSelection(new DefaultRandom(42));
        _tournament = new TournamentSelection(2, new DefaultRandom(42));
    }

    // Operators and output buffers are reused across generations by the engine.
    private void Select(ISelection selection)
    {
        _parents.Clear();
        selection.Process(_population, _parents, PopulationSize);
    }

    [Benchmark]
    public void BoltzmannSelection() => Select(_boltzmann);

    [Benchmark]
    public void EliteSelection() => Select(_elite);

    [Benchmark]
    public void RandomSelection() => Select(_random);

    [Benchmark]
    public void RandomSelectionWithoutDuplicates() => Select(_uniqueRandom);

    [Benchmark]
    public void RankSelection() => Select(_rank);

    [Benchmark]
    public void RouletteWheelSelection() => Select(_roulette);

    [Benchmark]
    public void StochasticUniversalSamplingSelection() => Select(_sus);

    [Benchmark]
    public void TournamentSelection() => Select(_tournament);
}