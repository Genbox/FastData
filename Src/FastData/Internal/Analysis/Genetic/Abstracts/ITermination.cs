namespace Genbox.FastData.Internal.Analysis.Genetic.Abstracts;

internal interface ITermination
{
    bool ShouldTerminate(int evolutions, double fitness);
}