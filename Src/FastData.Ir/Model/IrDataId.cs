namespace Genbox.FastData.Ir.Model;

/// <summary>A typed, scoped identity for a data declaration.</summary>
public readonly record struct IrDataId(int Value, IrType Type);