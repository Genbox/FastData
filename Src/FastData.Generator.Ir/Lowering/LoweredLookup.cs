using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Lowering;

/// <summary>A lowered lookup program and the storage a backend may expose directly.</summary>
/// <param name="Lookup">The validated lookup program.</param>
/// <param name="KeyStorage">A plain key array in slot order, when the structure keeps one.</param>
/// <param name="ValueStorage">A plain value array containing one element per entry, when available.</param>
internal sealed record LoweredLookup(FastDataProgram Lookup, IrDataId? KeyStorage, IrDataId? ValueStorage);