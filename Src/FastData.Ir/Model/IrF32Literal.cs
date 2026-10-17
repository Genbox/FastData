using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>A binary32 literal preserving exact bits, including signed zero and NaN payloads.</summary>
public sealed record IrF32Literal(uint Bits) : IrLiteral
{
    public override IrType Type => IrType.F32;
    public float Value => BitConverter.UInt32BitsToSingle(Bits);

    /// <summary>Captures a binary32 value as its exact bits.</summary>
    public static IrF32Literal FromValue(float value) => new IrF32Literal(BitConverter.SingleToUInt32Bits(value));
}