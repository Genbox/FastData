using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

public sealed class IrAssignmentStatement : IrStatement
{
    public IrAssignmentStatement(IrVariableId variableId, IrExpression value)
    {
        Require(variableId.Type == value.Type, "Expression type is incompatible with its destination.", code: IrDiagnosticCode.TypeMismatch);
        VariableId = variableId;
        Value = value;
    }

    public IrVariableId VariableId { get; }
    public IrExpression Value { get; }
}