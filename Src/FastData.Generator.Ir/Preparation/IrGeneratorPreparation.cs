using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;

namespace Genbox.FastData.Generator.Ir.Preparation;

/// <summary>Lowers frontend structures, expands unsupported builtins, and prepares backend control flow.</summary>
public static class IrGeneratorPreparation
{
    /// <summary>Builds a prepared lookup for the supplied backend, preserving a validated core program for reference execution.</summary>
    public static PreparedLookup Prepare<TKey, TValue>(GeneratorConfigBase config, IContext context, IrBackendCapabilities capabilities)
    {
        if (config is StringGeneratorConfig text && text.Encoding != capabilities.Encoding)
            throw new ArgumentException("Frontend and backend text encodings must match.", nameof(capabilities));

        LoweredLookup lowered = ContextLowerer.Lower<TKey, TValue>(config, context, capabilities);
        return Prepare(lowered, capabilities, config.StructureType.ToString());
    }

    /// <summary>Prepares a validated lookup built with the target's text encoding and builtin capabilities.</summary>
    public static PreparedLookup Prepare(FastDataProgram lookup, IrBackendCapabilities capabilities)
    {
        if (lookup.Program.TextEncoding != capabilities.TextEncoding)
            throw new ArgumentException("Program and backend text encodings must match.", nameof(capabilities));

        return Prepare(new LoweredLookup(lookup, null, null), capabilities, "Custom");
    }

    private static PreparedLookup Prepare(LoweredLookup lowered, IrBackendCapabilities capabilities, string structureName)
    {
        PreparedFunctionGroup main = new FunctionPreparation(lowered.Lookup.Program, capabilities, "fdFunction").Run();
        IrBuiltinOperation fallbacks = main.RequiredBuiltins & capabilities.RuntimeFallbacks;
        PreparedFunctionGroup? runtime = fallbacks == IrBuiltinOperation.None ? null : PrepareRuntime(fallbacks, capabilities);
        PreparedLookupApi api = new PreparedLookupApi(lowered.Lookup, lowered.KeyStorage, lowered.ValueStorage, main.Names);
        IReadOnlyList<PreparedFunction> functions = runtime == null ? main.Functions : main.Functions.Concat(runtime.Functions).ToArray();
        IrBuiltinOperation required = main.RequiredBuiltins | (runtime?.RequiredBuiltins ?? IrBuiltinOperation.None);
        return new PreparedLookup(lowered.Lookup, api, functions, required, (runtime ?? main).EntryPoints, structureName);
    }

    private static PreparedFunctionGroup PrepareRuntime(IrBuiltinOperation operations, IrBackendCapabilities capabilities)
    {
        IrProgramBuilder builder = new IrProgramBuilder(capabilities.TextEncoding);
        IrBuiltinBuilder builtins = new IrBuiltinBuilder(builder, IrBuiltins.Required);
        Dictionary<IrBuiltinOperation, IrFunctionId> entryPoints = new Dictionary<IrBuiltinOperation, IrFunctionId>();

        foreach (IrBuiltinOperation operation in Enum.GetValues<IrBuiltinOperation>())
        {
            if (operation != IrBuiltinOperation.None && operations.HasFlag(operation))
                entryPoints.Add(operation, builtins.Declare(operation));
        }

        return new FunctionPreparation(builder.Build(), capabilities, "fdRuntime").Run(entryPoints);
    }
}