using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A prepared dispatch whose distinct cases return values; a miss continues after the switch.</summary>
/// <remarks>An emission node for backend dispatch. The original lookup program supports reference execution.</remarks>
public sealed class PreparedSwitch : IrStatement
{
    internal PreparedSwitch(IrVariableExpression value, IReadOnlyList<PreparedSwitchCase> cases)
    {
        Value = value;
        Cases = cases;
    }

    /// <summary>The variable dispatched without re-evaluating a computation.</summary>
    public IrVariableExpression Value { get; }

    /// <summary>Distinct constant labels and their return values, in original order.</summary>
    public IReadOnlyList<PreparedSwitchCase> Cases { get; }
}