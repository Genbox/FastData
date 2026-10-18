namespace Genbox.FastData.Generator.Ir.Operations;

/// <summary>Target-neutral numeric and code-unit primitives.</summary>
/// <remarks>
/// Each operation is one bit, so a value can also describe the set of operations a generator supports.
/// The enum declares <see cref="None" /> and single operations; composite sets are on <see cref="IrBuiltins" />.
/// Every String operand must be non-null.
/// </remarks>
[Flags]
public enum IrBuiltinOperation
{
    /// <summary>The empty operation set.</summary>
    None = 0,
    /// <summary>U8 (UTF-8) or U16 (UTF-16) unit at a nonnegative in-bounds offset.</summary>
    TextUnitAt = 1 << 0,
    /// <summary>I32 number of code units in the program's text encoding.</summary>
    TextLength = 1 << 1,
    /// <summary>Encoded-unit fragment equality at a nonnegative offset; the fragment must fit within the input. An empty fragment may start at the end.</summary>
    TextEqualsAt = 1 << 2,
    /// <summary><see langword="true" /> if every code unit is below 128, including for an empty string.</summary>
    TextIsAscii = 1 << 3,
    /// <summary>Unsigned byte at a nonnegative in-bounds byte offset into the little-endian bytes of the code units, zero-extended to U32.</summary>
    ReadU8 = 1 << 4,
    /// <summary>Unaligned little-endian U16 at a nonnegative byte offset whose 2 bytes are in bounds, zero-extended to U32.</summary>
    ReadU16LittleEndian = 1 << 5,
    /// <summary>Unaligned little-endian U32 at a nonnegative byte offset whose 4 bytes are in bounds.</summary>
    ReadU32LittleEndian = 1 << 6,
    /// <summary>Unaligned little-endian U64 at a nonnegative byte offset whose 8 bytes are in bounds.</summary>
    ReadU64LittleEndian = 1 << 7,
    /// <summary>Upper 64 bits of the product of two U64 operands.</summary>
    MultiplyHigh = 1 << 8,
    /// <summary>Number of set bits in a U64, returned as I32.</summary>
    PopCount = 1 << 9,
    /// <summary>Number of trailing zero bits in a U64, returned as I32; 64 for zero.</summary>
    TrailingZeroCount = 1 << 10
}