using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A validated lookup and its emission plan, shared by IR backends.</summary>
public sealed class PreparedLookup
{
    internal PreparedLookup(FastDataProgram lookup, PreparedLookupApi api, IReadOnlyList<PreparedFunction> functions, IrBuiltinOperation requiredBuiltins, IReadOnlyDictionary<IrBuiltinOperation, string> runtimeFallbacks, string structureName)
    {
        StructureName = structureName;
        Lookup = lookup;
        RequiredBuiltins = requiredBuiltins;
        Api = api;
        Records = lookup.Program.Records.Select((record, i) => new PreparedRecord(PreparedNames.Name("fdRecord", i), record)).ToArray();
        Data = PrepareData(lookup);
        Functions = functions;
        RuntimeFallbacks = runtimeFallbacks;
    }

    public PreparedLookupApi Api { get; }

    /// <summary>Records in original declaration order, indexed by record ID.</summary>
    public IReadOnlyList<PreparedRecord> Records { get; }

    /// <summary>Data in original declaration order, indexed by data ID.</summary>
    public IReadOnlyList<PreparedData> Data { get; }

    /// <summary>Lookup functions first, followed by any portable runtime helpers.</summary>
    public IReadOnlyList<PreparedFunction> Functions { get; }

    /// <summary>Resolved names for runtime-dependent builtin fallbacks.</summary>
    public IReadOnlyDictionary<IrBuiltinOperation, string> RuntimeFallbacks { get; }

    /// <summary>The selected lookup structure, for generated source descriptions.</summary>
    public string StructureName { get; }

    /// <summary>The original validated lookup, suitable for interpretation.</summary>
    public FastDataProgram Lookup { get; }

    /// <summary>Builtin operations required by the prepared functions, including operations backed by runtime fallback helpers.</summary>
    public IrBuiltinOperation RequiredBuiltins { get; }

    /// <summary>Returns the emitted name of a data declaration.</summary>
    public string DataName(IrDataId data) => Data[data.Value].Name;

    /// <summary>Returns the emitted name of a record field.</summary>
    public string FieldName(IrFieldId field) => Records[field.RecordId.Value].Fields[field.Value].Name;

    private static PreparedData[] PrepareData(FastDataProgram lookup)
    {
        HashSet<IrDataId> exports = new HashSet<IrDataId>(lookup.Contract.Constants);
        return lookup.Program.Data.Select((data, i) =>
        {
            bool exported = exports.Contains(new IrDataId(i, data.Type));
            string name = exported ? data.Name : PreparedNames.Name("fdData", i);
            return new PreparedData(name, data.Value, exported);
        }).ToArray();
    }
}