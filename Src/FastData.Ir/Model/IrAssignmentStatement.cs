using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

public sealed class IrAssignmentStatement(IrVariableId variableId, IrExpression value) : IrStatement
{
    public IrVariableId VariableId { get; } = variableId;
    public IrExpression Value { get; } = value;
}