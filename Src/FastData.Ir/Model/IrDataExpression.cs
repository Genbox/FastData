using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>Reads an immutable data declaration.</summary>
public sealed class IrDataExpression(IrDataId dataId) : IrExpression
{
    public IrDataId DataId { get; } = dataId;
}