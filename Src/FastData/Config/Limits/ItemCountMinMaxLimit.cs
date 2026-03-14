namespace Genbox.FastData.Config.Limits;

public class ItemCountMinMaxLimit(uint MinCount, uint MaxCount) : ILimit<uint>
{
    public bool IsWithinLimit(uint value) => true;
}