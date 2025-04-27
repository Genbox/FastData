namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

internal static class GeneHelper
{
    internal static int[] GetSortedByFitness(StaticArray<Entity> population)
    {
        int[] indices = new int[population.Count];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        Array.Sort(indices, (a, b) => population[b].Fitness.CompareTo(population[a].Fitness));

        return indices;
    }
}