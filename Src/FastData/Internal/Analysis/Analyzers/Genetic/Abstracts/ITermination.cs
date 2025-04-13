namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;

internal interface ITermination
{
    bool Process(int evolutions, double fitness);
}