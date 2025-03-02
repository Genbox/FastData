// using Genbox.FastData.Internal.Analysis.Genetic.Abstracts;
//
// namespace Genbox.FastData.Internal.Analysis.Genetic.SelectionPolicy;
//
// internal class MultiSelectionPolicy(IEnumerable<(ISelection, bool)> items) : ISelectionPolicy
// {
//     private readonly (ISelection, bool)[] _items = items.ToArray();
//
//     public void Select(int generation, Candidate<GeneticHashSpec>[] population)
//     {
//         foreach ((ISelection selection, bool selected) in _items)
//         {
//             foreach (int candidate in selection.Select(generation, population))
//                 population[candidate].Selected = selected;
//         }
//     }
// }