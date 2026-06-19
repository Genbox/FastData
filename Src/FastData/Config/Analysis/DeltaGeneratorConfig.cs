using JetBrains.Annotations;

namespace Genbox.FastData.Config.Analysis;

[PublicAPI]
public sealed class DeltaGeneratorConfig
{
    /// <summary>Maximum segment length emitted from each delta run. A value of <c>-1</c> keeps generated segments within the shortest encoded key length; positive values allow segments beyond it.</summary>
    public int MaxSegmentLength
    {
        get;
        set
        {
            if (value is < -1 or 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Max segment length must be -1 or greater than zero.");

            field = value;
        }
    } = -1;
}