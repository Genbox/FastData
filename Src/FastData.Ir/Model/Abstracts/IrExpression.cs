using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Ir.Model.Abstracts;

/// <summary>A value computation with single evaluation of each required operand per parent evaluation.</summary>
/// <remarks>
/// Nodes and their retained collections and buffers must remain immutable after construction, including through caller-held aliases. Immutable nodes may be shared.
/// Eager operands, call arguments, and aggregate receivers/indices evaluate exactly once in an unspecified order.
/// Logical expressions evaluate Left once and Right only when required; value selection uses ordered if statements.
/// Producers must ensure nonzero integral divisors, non-null dereferenced receivers, and in-bounds accesses on executed paths.
/// Signed arithmetic results must be representable, shift counts must be within the operand width, and unit-to-character conversions must fit the character kind.
/// Precondition violations have unspecified portable behavior; the interpreter supplies execution diagnostics.
/// </remarks>
[SuppressMessage("Naming", "CA1715:Identifiers should have correct prefix", Justification = "The name intentionally follows the Ir-prefixed node family.")]
public interface IrExpression
{
    /// <summary>The result type of this expression.</summary>
    IrType Type { get; }
}