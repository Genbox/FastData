namespace Genbox.FastData.Ir.Model.Abstracts;

/// <summary>An immutable typed value that can be used directly as an expression.</summary>
public abstract record IrLiteral : IrExpression
{
    private protected IrLiteral() {}

    public abstract IrType Type { get; }
}