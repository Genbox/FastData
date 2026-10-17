namespace Genbox.FastData.Ir.Internal;

/// <summary>Exact binary32/binary64 bit representations and canonical NaN constants.</summary>
internal static class IrFloatingPointBits
{
    public const uint F32CanonicalNaN = 0x7FC00000;
    public const ulong F64CanonicalNaN = 0x7FF8000000000000;
}