using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Ir.Model.Abstracts;

/// <summary>A defined function or external declaration. Its identity is its position in <see cref="IrProgram.Callables" />.</summary>
[SuppressMessage("Naming", "CA1715:Identifiers should have correct prefix", Justification = "The name intentionally follows the Ir-prefixed node family.")]
public interface IrCallable
{
    /// <summary>The value-returning callable signature.</summary>
    IrFunctionSignature Signature { get; }
}