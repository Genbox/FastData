using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

public sealed record IrNullLiteral(IrType Type) : IrLiteral
{
    public override IrType Type { get; } = Type.AcceptsNull ? Type : throw Fail("The literal type does not admit null.", code: IrDiagnosticCode.TypeMismatch);
}