namespace Genbox.FastData.Ir.Model.Abstracts;

/// <summary>An immutable typed value that can be used directly as an expression.</summary>
/// <remarks>Collection and buffer storage is immutable by ownership transfer, rather than defensive copying. Callers must not mutate it through any alias.</remarks>
public abstract record IrLiteral : IrExpression
{
    private protected IrLiteral() {}

    public abstract IrType Type { get; }
}