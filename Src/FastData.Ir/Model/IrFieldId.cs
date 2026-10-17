using System.Runtime.InteropServices;

namespace Genbox.FastData.Ir.Model;

/// <summary>A field's position within its owning record.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct IrFieldId(IrRecordId RecordId, int Value);