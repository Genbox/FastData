namespace Genbox.FastData.Ir.Model;

/// <summary>Program-local identity of immutable data, distinct from writable variables.</summary>
public readonly record struct IrDataId(int Value);