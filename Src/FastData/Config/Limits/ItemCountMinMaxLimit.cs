namespace Genbox.FastData.Config.Limits;

/// <summary>Constrains an item count to an inclusive range.</summary>
/// <param name="MinCount">The inclusive minimum item count.</param>
/// <param name="MaxCount">The inclusive maximum item count.</param>
public class ItemCountMinMaxLimit(uint MinCount, uint MaxCount) : ILimit<uint>
{
    /// <inheritdoc />
    public bool IsWithinLimit(uint value) => value >= MinCount && value <= MaxCount;
}