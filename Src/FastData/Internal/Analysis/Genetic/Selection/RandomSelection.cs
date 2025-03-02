using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Selects candidates at random. It may return duplicates if unique is not set
/// </summary>
/// <param name="unique">If set, it shuffles the order instead of selecting. This ensures no duplicates.</param>
/// <param name="seed">RNG seed for deterministic simulations. Set to 0 for random seed.</param>
internal sealed class RandomSelection(bool unique, int seed = 0) : ISelection
{
    private readonly Random _random = seed != 0 ? new Random(seed) : new Random();

    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population) => unique
        ? Enumerable.Range(0, population.Length).OrderBy(_ => _random.Next())
        : Enumerable.Range(0, population.Length).Select(_ => _random.Next(population.Length));
}