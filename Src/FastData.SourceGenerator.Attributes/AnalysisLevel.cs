namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Controls how much string-hash analysis the source generator performs.</summary>
public enum AnalysisLevel : byte
{
    /// <summary>Use the default analysis budget.</summary>
    Balanced = 0,

    /// <summary>Disable string-hash analysis and use the default hash expression.</summary>
    Disabled,

    /// <summary>Use a small analysis budget suitable for faster builds.</summary>
    Fast,

    /// <summary>Use a larger analysis budget that may improve generated lookup quality at the cost of generation time.</summary>
    Aggressive
}