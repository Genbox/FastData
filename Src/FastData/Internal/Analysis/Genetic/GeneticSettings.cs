namespace Genbox.FastData.Internal.Analysis.Genetic;

internal class GeneticSettings
{
    internal int PopulationSize { get; set; } = 10;
    internal int MaxEvolutions { get; set; } = 1000;

    internal bool CrossEliteOnly { get; set; } = true;
    internal float CrossPercent { get; set; } = 0.25f;
    internal double TimeWeight { get; set; } = 1.0;
    internal double FillWeight { get; set; } = 1.0;

    internal double CapacityFactor { get; set; } = 1.0;

    internal bool StagnantTerminate { get; set; } = true;
    internal byte StagnantTopResults { get; set; } = 3;
    internal double StagnantPercent { get; set; } = 0.0001;
}