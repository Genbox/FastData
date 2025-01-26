namespace Genbox.FastData.Internal.Analysis.Genetic;

public class GeneticSettings
{
    internal int PopulationSize { get; set; } = 10;
    internal int MaxEvolutions { get; set; } = 1000;

    internal bool CrossEliteOnly { get; set; } = false;
    internal float CrossPercent { get; set; } = 0.5f;
    internal double TimeWeight { get; set; } = 1.0;
    internal double FillWeight { get; set; } = 1.0;

    internal double CapacityFactor { get; set; } = 1.0;
    internal bool StagnantTerminate { get; set; } = true;
    internal double StagnantPercent { get; set; } = 0.0001;
}