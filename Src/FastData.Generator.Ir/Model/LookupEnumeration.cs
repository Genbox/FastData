using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Model;

/// <summary>Lazy enumeration through ordinary functions: Next(I64) -> I64, Key(I64) -> TKey, and optionally Value(I64) -> TValue.</summary>
/// <remarks>
/// Positions are nonnegative opaque cursors. Next(-1) returns the first position, and Next returns -1 after the last one.
/// Keys and values share the cursor, so position p yields the key and value of the same entry. Cursor advancement operates on positions; key materialization belongs to the key function.
/// </remarks>
/// <param name="NextFunctionId">Returns the position after its argument, or -1 at the end; -1 as the argument starts the enumeration.</param>
/// <param name="KeyFunctionId">Returns the key at a position.</param>
/// <param name="ValueFunctionId">Returns the value at a position, or <see langword="null" /> for key-only enumeration.</param>
public sealed record LookupEnumeration(IrFunctionId NextFunctionId, IrFunctionId KeyFunctionId, IrFunctionId? ValueFunctionId = null);