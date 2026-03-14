using Genbox.FastData.Config.Analysis;

namespace Genbox.FastData.Config;

public sealed class StringDataConfig : DataConfig
{
    /// <summary>Enable case-insensitive lookups for string keys.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Enable trimming of common prefix and suffix across string keys.</summary>
    public bool EnablePrefixSuffixTrimming { get; set; }

    /// <summary>Configuration for analyzers. Set to null to disable analysis.</summary>
    public StringAnalyzerConfig? StringAnalyzerConfig { get; set; }
}