using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

internal struct Entity
{
    internal Entity(int geneCount)
    {
        Genes = new IGene[geneCount];
    }

    internal Entity(IGene[] genes) : this(genes.Length)
    {
        for (int i = 0; i < genes.Length; i++)
            Genes[i] = genes[i].Clone();
    }

    internal double Fitness { get; set; }
    internal IGene[] Genes { get; }
    internal int Tag { get; set; }

    internal readonly void ForceMutate(IRandom random)
    {
        foreach (IGene gene in Genes)
            gene.Mutate(random);
    }

    internal readonly bool HasSameGenes(Entity other)
    {
        if (Genes.Length != other.Genes.Length)
            return false;

        for (int i = 0; i < Genes.Length; i++)
        {
            if (!Genes[i].HasSameValue(other.Genes[i]))
                return false;
        }

        return true;
    }
}