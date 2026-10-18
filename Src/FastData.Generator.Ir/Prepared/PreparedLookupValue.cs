using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A typed expression supplying a lookup API value.</summary>
public sealed record PreparedLookupValue(IrType Type, IrExpression Expression);