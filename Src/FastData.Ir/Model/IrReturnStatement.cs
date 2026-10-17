using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed class IrReturnStatement(IrExpression value) : IrStatement
{
    public IrExpression Value { get; } = value;
}