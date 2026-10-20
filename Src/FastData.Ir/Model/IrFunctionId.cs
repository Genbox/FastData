namespace Genbox.FastData.Ir.Model;

/// <summary>A typed, scoped identity for a function declaration.</summary>
public readonly record struct IrFunctionId(int Value, IrType ReturnType);