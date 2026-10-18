using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>One constant label and its return expression.</summary>
public sealed record PreparedSwitchCase(IrLiteral Label, IrExpression Result);