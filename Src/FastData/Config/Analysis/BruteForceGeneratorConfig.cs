using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class BruteForceGeneratorConfig
{
    /// <summary>Maximum segment length emitted by the generator.</summary>
    public int MaxSegmentLength
    {
        get;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Max segment length must be greater than zero.");

            field = value;
        }
    } = 8;
}