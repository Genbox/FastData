namespace Genbox.FastData.Ir.Model;

/// <summary>A typed, scoped identity for a variable declaration.</summary>
public readonly record struct IrVariableId(int Value, IrType Type);