using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>The lookup, storage plan, and extension syntax for one rendered scope.</summary>
public sealed class EmissionContext(PreparedLookup lookup, PreparedEmissionPlan? plan = null, Func<IrExpression, string[], string>? customExpression = null)
{
    public PreparedLookup Lookup { get; } = lookup;
    public PreparedEmissionPlan Plan { get; } = plan ?? lookup.EmissionPlan;
    public Func<IrExpression, string[], string>? CustomExpression { get; } = customExpression;
}