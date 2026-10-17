using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>A binary64 literal preserving exact bits, including signed zero and NaN payloads.</summary>
public sealed record IrF64Literal(ulong Bits) : IrLiteral
{
    public override IrType Type => IrType.F64;
    public double Value => BitConverter.UInt64BitsToDouble(Bits);

    /// <summary>Captures a binary64 value as its exact bits.</summary>
    public static IrF64Literal FromValue(double value) => new IrF64Literal(BitConverter.DoubleToUInt64Bits(value));
}