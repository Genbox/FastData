namespace Genbox.FastData.SourceGenerator.Attributes;

public enum AnalysisLevel : byte
{
    Balanced = 0, // We default to balanced
    Disabled,
    Fast,
    Aggressive
}