using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

/// <summary>Configures string-hash analysis based on selected positions and key length.</summary>
[PublicAPI]
public sealed class PositionLengthAnalyzerConfig : IAnalyzerConfig
{
    /// <summary>Gets or sets a value indicating whether key length contributes to the hash.</summary>
    public bool IncludeLength { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the last character may contribute to the hash.</summary>
    public bool IncludeLastChar { get; set; } = true;
}