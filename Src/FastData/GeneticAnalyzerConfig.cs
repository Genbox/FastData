namespace Genbox.FastData;

public sealed class GeneticAnalyzerConfig : AnalyzerConfig
{
    public int PopulationSize { get; set; } = 10;
    public int MaxEvolutions { get; set; } = 1000;

    public bool CrossEliteOnly { get; set; } = true;
    public float CrossPercent { get; set; } = 0.25f;

    public bool StagnantTerminate { get; set; } = true;
    public byte StagnantTopResults { get; set; } = 3;
    public double StagnantPercent { get; set; } = 0.0001;
}