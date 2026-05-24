using Genbox.FastData.Config.Analysis;

namespace Genbox.FastData.Config;

/// <summary>Configuration for generating string-key lookup structures.</summary>
public sealed class StringDataConfig : DataConfig
{
    /// <summary>Enable case-insensitive lookups for string keys.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Configuration for analyzers. Set to null to disable analysis.</summary>
    public StringAnalyzerConfig? StringAnalyzerConfig { get; set; }
}