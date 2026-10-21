using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Prepared;

/// <summary>A validated lookup and its emission plan, shared by IR backends.</summary>
public sealed class PreparedLookup
{
    internal PreparedLookup(FastDataProgram lookup, IReadOnlyList<PreparedFunction> functions, IReadOnlyList<PreparedCall> calls, IrBuiltinOperation requiredBuiltins, string structureName, GeneratorEncoding encoding, IrDataId? keyStorage = null, IrDataId? valueStorage = null)
    {
        StructureName = structureName;
        Encoding = encoding;
        Lookup = lookup;
        RequiredBuiltins = requiredBuiltins;
        Calls = calls;
        KeyStorage = keyStorage;
        ValueStorage = valueStorage;
        Records = lookup.Program.Records.Select((record, i) => new PreparedRecord(PreparedNames.Name("fdRecord", i), record)).ToArray();
        Data = PrepareData(lookup);
        EmissionPlan = PreparedEmissionPlan.Create(Data, functions);
    }

    /// <summary>The configured encoding that determines generated text APIs and storage.</summary>
    public GeneratorEncoding Encoding { get; }

    public IReadOnlyList<PreparedCall> Calls { get; }
    public IrDataId? KeyStorage { get; }
    public IrDataId? ValueStorage { get; }

    /// <summary>Records in original declaration order, indexed by record ID.</summary>
    public IReadOnlyList<PreparedRecord> Records { get; }

    /// <summary>Data in original declaration order, indexed by data ID.</summary>
    public IReadOnlyList<PreparedData> Data { get; }

    /// <summary>Prepared lookup functions, including any builtin implementations lowered into IR.</summary>
    public IReadOnlyList<PreparedFunction> Functions => EmissionPlan.Functions;

    /// <summary>Complete storage and function plan, ready for rendering without further preparation.</summary>
    public PreparedEmissionPlan EmissionPlan { get; }

    /// <summary>The selected lookup structure, for generated source descriptions.</summary>
    public string StructureName { get; }

    /// <summary>The original validated lookup, suitable for interpretation.</summary>
    public FastDataProgram Lookup { get; }

    /// <summary>Builtin operations implemented by the backend and required by the prepared functions.</summary>
    public IrBuiltinOperation RequiredBuiltins { get; }

    /// <summary>Returns the emitted function name, including any builtin fallback alias.</summary>
    public string FunctionName(IrFunctionId function) => Calls[function.Value].Name;

    /// <summary>Returns the emitted name of a data declaration.</summary>
    public string DataName(IrDataId data) => Data[data.Value].Name;

    /// <summary>Returns the emitted name of a record field.</summary>
    public string FieldName(IrFieldId field) => Records[field.RecordId.Value].FieldNames[field.Value];

    private static PreparedData[] PrepareData(FastDataProgram lookup)
    {
        HashSet<int> exports = new HashSet<int>(lookup.Contract.Constants.Select(static data => data.Value));
        return lookup.Program.Data.Select((data, i) =>
        {
            bool exported = exports.Contains(i);
            string name = exported ? data.Name : PreparedNames.Name("fdData", i);
            return new PreparedData(name, data.Value, exported);
        }).ToArray();
    }
}