namespace Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

internal interface IMutation
{
    void Mutate(Candidate<GeneticHashSpec>[] population, List<int> children);
}