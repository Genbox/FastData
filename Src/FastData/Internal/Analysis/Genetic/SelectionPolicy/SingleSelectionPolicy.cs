// using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
//
// namespace Genbox.FastData.Internal.Analysis.Genetic.SelectionPolicy;
//
// internal class SingleSelectionPolicy(ISelection selection, bool selected) : ISelectionPolicy
// {
//     public void Select(int generation, Candidate<GeneticHashSpec>[] population)
//     {
//         foreach (int candidate in selection.Select(generation, population))
//             population[candidate].Selected = selected;
//     }
// }