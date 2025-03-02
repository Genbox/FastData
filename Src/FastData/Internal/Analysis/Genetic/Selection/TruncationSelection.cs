using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Genetic.Selection;

/// <summary>
/// Truncation Selection is a deterministic selection method where only the top X% of individuals (highest fitness) are selected as parents for reproduction, while the rest are discarded. This creates strong selection pressure and fast convergence but risks premature convergence if diversity is lost too quickly.
/// </summary>
/// <param name="topPercent">The top percent to take. It must be a number between 0 and 1</param>
internal sealed class TruncationSelection(double topPercent) : ISelection
{
    public IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population)
    {
        int[] indices = Enumerable.Range(0, population.Length).ToArray();
        int parentCount = (int)(population.Length * topPercent);

        Array.Sort(indices, 0, indices.Length, Comparer<int>.Create((a, b) => population[b].Fitness.CompareTo(population[a].Fitness)));

        for (int i = 0; i < parentCount; i++)
            yield return indices[i];
    }
}