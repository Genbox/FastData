using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

/// <summary>Configures genetic search for string-hash candidates.</summary>
[PublicAPI]
public sealed class GeneticAnalyzerConfig : IAnalyzerConfig
{
    /// <summary>Gets or sets a value indicating whether selected parents are shuffled before reproduction.</summary>
    public bool ShuffleParents { get; set; }

    /// <summary>Gets or sets the number of candidates maintained in each generation.</summary>
    public int PopulationSize { get; set; } = 32;

    /// <summary>Gets or sets the maximum number of generations to evaluate.</summary>
    public int MaxGenerations { get; set; } = 10;

    /// <summary>Gets or sets the random seed. Set to <c>0</c> to use a new seed for each run.</summary>
    public int RandomSeed { get; set; }
}