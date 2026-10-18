using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Preparation;

internal sealed class PreparedFunctionGroup
{
    private static readonly IReadOnlyDictionary<IrBuiltinOperation, string> _emptyEntryPoints = new Dictionary<IrBuiltinOperation, string>();

    internal PreparedFunctionGroup(IReadOnlyList<PreparedFunction> functions, IReadOnlyList<string> names, IrBuiltinOperation requiredBuiltins, IReadOnlyDictionary<IrBuiltinOperation, IrFunctionId>? entryPoints = null)
    {
        Functions = functions;
        Names = names;
        RequiredBuiltins = requiredBuiltins;
        EntryPoints = entryPoints?.ToDictionary(entry => entry.Key, entry => names[entry.Value.Value]) ?? _emptyEntryPoints;
    }

    public IReadOnlyList<PreparedFunction> Functions { get; }
    public IReadOnlyList<string> Names { get; }
    public IrBuiltinOperation RequiredBuiltins { get; }

    /// <summary>Generated function names implementing runtime fallback operations.</summary>
    public IReadOnlyDictionary<IrBuiltinOperation, string> EntryPoints { get; }
}