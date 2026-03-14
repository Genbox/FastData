namespace Genbox.FastData.Config.Limits;

public record StringLengthMinMaxLimit(uint MinLength, uint MaxLength) : ILimit<uint>
{
    public bool IsWithinLimit(uint value) => true;
}