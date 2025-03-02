namespace Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

internal interface ISelection
{
    IEnumerable<int> Select(int generation, Candidate<GeneticHashSpec>[] population);
}