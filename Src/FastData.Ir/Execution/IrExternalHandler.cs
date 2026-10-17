using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Execution;

/// <summary>Executes a call to an external function during interpretation.</summary>
/// <remarks>Handlers must satisfy <see cref="IrExternalFunction" />'s purity and runtime-precondition contract, and return a literal matching the declared return type and valid for the program's text encoding and declarations.</remarks>
/// <param name="function">The external declaration being called.</param>
/// <param name="arguments">Already-evaluated values in parameter order. Typed nulls are represented by null literals.</param>
/// <returns>The returned literal, including an explicit typed null literal for an actual null value.</returns>
public delegate IrLiteral IrExternalHandler(IrExternalFunction function, IReadOnlyList<IrLiteral> arguments);