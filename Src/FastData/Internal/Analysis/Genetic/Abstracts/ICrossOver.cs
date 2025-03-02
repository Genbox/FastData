namespace Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

internal interface ICrossOver
{
    IEnumerable<GeneticHashSpec> Cross(Candidate<GeneticHashSpec>[] population, IEnumerable<int> parents);
}