using Genbox.FastData.Internal.Abstracts;
using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

/// <summary>Configures genetic search for string-hash candidates.</summary>
[PublicAPI]
public sealed class GeneticAnalyzerConfig : IAnalyzerConfig
{
    /// <summary>Gets or sets a value indicating whether selected parents are shuffled before reproduction.</summary>
    public bool ShuffleParents { get; set; }

    /// <summary>Gets or sets the number of candidates maintained in each generation. Must be at least 2.</summary>
    public int PopulationSize
    {
        get;
        set
        {
            if (value < 2)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Population size must be at least 2.");

            field = value;
        }
    } = 32;

    /// <summary>Gets or sets the maximum number of generations to evaluate. Must be greater than zero.</summary>
    public int MaxGenerations
    {
        get;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Max generations must be greater than zero.");

            field = value;
        }
    } = 10;

    /// <summary>Gets or sets the random seed. Set to <c>0</c> to use a new seed for each run.</summary>
    public int RandomSeed { get; set; }
}