using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A test for the presence or absence of a reference value.</summary>
public sealed record PreparedNullCheck(IrExpression Operand, bool IsNull) : IrExpression;