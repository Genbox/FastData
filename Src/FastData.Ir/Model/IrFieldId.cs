using System.Runtime.InteropServices;

namespace Genbox.FastData.Ir.Model;

/// <summary>A typed, scoped identity for a field declaration.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct IrFieldId(IrRecordId RecordId, int Value, IrType Type);