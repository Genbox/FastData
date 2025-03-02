namespace Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

internal interface ISelectionPolicy
{
    void Select(int generation, Candidate<GeneticHashSpec>[] population);
}