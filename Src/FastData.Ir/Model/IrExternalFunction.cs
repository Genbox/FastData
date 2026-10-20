using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Model;

/// <summary>A callable symbol with a fixed signature and an externally supplied implementation.</summary>
/// <remarks>
/// External implementations are pure functions of their argument values under the fixed program semantics.
/// Arguments satisfying the preconditions produce a deterministic value of the declared return type.
/// Results must be invariant under changes to call order or invocation count; required operands evaluate once.
/// Aggregate arguments are interpreted by their contents. Violating this purity contract has unspecified portable behavior.
/// Core validates signatures; producers establish implementation correctness and runtime preconditions.
/// </remarks>
public sealed class IrExternalFunction(string symbol, IrFunctionSignature signature) : IrCallable
{
    public string Symbol { get; } = symbol ?? throw Fail("A declaration name is required.");
    public IrFunctionSignature Signature { get; } = signature;
}