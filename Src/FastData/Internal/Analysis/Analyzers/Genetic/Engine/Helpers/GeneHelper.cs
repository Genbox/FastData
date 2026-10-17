namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

internal static class GeneHelper
{
    internal static int[] GetSortedByFitness(StaticArray<Entity> population)
    {
        int[] indices = new int[population.Count];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;

        Array.Sort(indices, (a, b) => population[b].Fitness.CompareTo(population[a].Fitness));

        return indices;
    }

    /// <summary>Finds the first cumulative weight greater than the draw, skipping zero-weight entries.</summary>
    internal static int FindSlice(double[] cumulative, int count, double value)
    {
        int low = 0;
        int high = count - 1;

        while (low < high)
        {
            int mid = low + ((high - low) / 2);

            if (cumulative[mid] > value)
                high = mid;
            else
                low = mid + 1;
        }

        return low;
    }

    /// <summary>Gets the weight of an entity in fitness-proportionate selection. Negative and NaN fitness gives no weight.</summary>
    internal static double GetSelectionWeight(double fitness) => fitness > 0 ? fitness : 0;
}