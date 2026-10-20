using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>Reads a function variable.</summary>
public sealed class IrVariableExpression(IrVariableId variableId) : IrExpression
{
    public IrVariableId VariableId { get; } = variableId;
    public IrType Type => VariableId.Type;
}