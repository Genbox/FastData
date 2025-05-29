using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

public struct Entity
{
    internal Entity(IGene[] genes)
    {
        Genes = new IGene[genes.Length];

        for (int i = 0; i < genes.Length; i++)
        {
            Genes[i] = genes[i].Clone();
        }
    }

    internal double Fitness { get; set; }
    internal IGene[] Genes { get; }
    internal object Tag { get; set; }

    internal readonly void ForceMutate(IRandom random)
    {
        foreach (IGene gene in Genes)
        {
            gene.Mutate(random);
        }
    }
}