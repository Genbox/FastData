using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Ir.Model.Abstracts;

/// <summary>A library-defined expression exposing its operands for structural traversal.</summary>
/// <remarks>Implementations must keep their operands and child collections immutable after construction, following the core IR ownership contract. Child traversal does not provide validation, execution, or rendering semantics.</remarks>
[SuppressMessage("Naming", "CA1715:Identifiers should have correct prefix", Justification = "The name intentionally follows the Ir-prefixed node family.")]
public interface IrExtensionExpression : IrExpression
{
    /// <summary>Operands in source order.</summary>
    IReadOnlyList<IrExpression> Children { get; }
}