namespace Genbox.FastData.Internal.Analysis.Genetic;

internal class GeneticSettings : CommonSettings
{
    internal int PopulationSize { get; set; } = 10;
    internal int MaxEvolutions { get; set; } = 1000;

    internal bool CrossEliteOnly { get; set; } = true;
    internal float CrossPercent { get; set; } = 0.25f;

    internal bool StagnantTerminate { get; set; } = true;
    internal byte StagnantTopResults { get; set; } = 3;
    internal double StagnantPercent { get; set; } = 0.0001;
}