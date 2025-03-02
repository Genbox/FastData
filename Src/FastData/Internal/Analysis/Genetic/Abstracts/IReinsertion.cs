namespace Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

internal interface IReinsertion
{
    void Reinsert(Candidate<GeneticHashSpec>[] population, List<int> parents);
}