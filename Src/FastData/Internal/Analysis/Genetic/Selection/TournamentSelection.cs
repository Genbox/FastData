using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Tournament Selection is a selection method in genetic algorithms where a small group of individuals (a tournament) is randomly chosen, and the best individual from the group is selected for reproduction. This process is repeated until the desired number of parents is selected.
/// </summary>
/// <param name="tournamentSize"></param>
/// <param name="seed">RNG seed for deterministic simulations. Set to 0 for random seed.</param>
internal sealed class TournamentSelection(int tournamentSize, int seed = 0) : ISelection
{
    private readonly Random _random = seed != 0 ? new Random(seed) : new Random();

    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        for (int i = 0; i < population.Length; i++)
        {
            int best = _random.Next(population.Length);

            for (int j = 1; j < tournamentSize; j++)
            {
                int challenger = _random.Next(population.Length);
                if (population[challenger].Fitness > population[best].Fitness)
                    best = challenger;
            }

            yield return best;
        }
    }
}