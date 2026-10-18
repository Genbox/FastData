using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A named field, parameter, or local ready for target declaration syntax.</summary>
public sealed record PreparedDeclaration(string Name, IrType Type);