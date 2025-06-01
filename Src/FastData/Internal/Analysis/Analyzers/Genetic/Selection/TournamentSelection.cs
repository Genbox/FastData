using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>Tournament Selection is a selection method in genetic algorithms where a small group of individuals (a tournament) is randomly chosen, and the best individual from the group is selected for reproduction. This process is repeated until the desired number of parents is selected.</summary>
internal sealed class TournamentSelection(int tournamentSize, IRandom random) : ISelection
{
    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        for (int i = 0; i < maxParents; i++)
        {
            int best = random.Next(population.Count);

            for (int j = 1; j < tournamentSize; j++)
            {
                int challenger = random.Next(population.Count);
                if (population[challenger].Fitness > population[best].Fitness)
                    best = challenger;
            }

            parents.Add(best);
        }
    }
}