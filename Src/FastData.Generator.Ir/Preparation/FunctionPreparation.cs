using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Preparation;

internal static class FunctionPreparation
{
    public static (IReadOnlyList<PreparedFunction> Functions, IReadOnlyList<PreparedCall> Calls, IrBuiltinOperation RequiredBuiltins) Run(IrProgram program, string namePrefix, IrBuiltinOperation supportedBuiltins)
    {
        IrBuiltins.ValidateSupport(supportedBuiltins);
        IReadOnlyList<IrCallable> callables = program.Callables;
        PreparedCall[] calls = new PreparedCall[callables.Count];
        IReadOnlyList<PreparedFunction> fallbackFunctions = [];
        IrBuiltinOperation requiredBuiltins = IrBuiltinOperation.None;

        IrProgramBuilder? fallbackProgram = null;
        IrBuiltinBuilder? fallbackBuilder = null;
        List<(int OriginalId, IrFunctionId FunctionId)> fallbacks = [];

        for (int i = 0; i < callables.Count; i++)
        {
            IrBuiltinOperation operation = IrBuiltinOperation.None;

            if (callables[i] is IrExternalFunction external)
            {
                // Resolve each external once, before binding any calls to it.
                if (!IrBuiltins.TryGetOperation(external.Symbol, out operation))
                    throw new NotSupportedException($"Unknown external function '{external.Symbol}'.");

                if (supportedBuiltins.HasFlag(operation))
                    requiredBuiltins |= operation;
                else
                {
                    fallbackProgram ??= new IrProgramBuilder(program.TextEncoding);
                    fallbackBuilder ??= new IrBuiltinBuilder(fallbackProgram, supportedBuiltins);
                    fallbacks.Add((i, fallbackBuilder.Declare(operation)));
                    operation = IrBuiltinOperation.None;
                }
            }

            calls[i] = new PreparedCall(PreparedNames.Name(namePrefix, i), operation);
        }

        if (fallbackProgram != null)
        {
            (IReadOnlyList<PreparedFunction> prepared, IReadOnlyList<PreparedCall> fallbackCalls, IrBuiltinOperation required) = Run(fallbackProgram.Build(), namePrefix + "Builtin", supportedBuiltins);
            requiredBuiltins |= required;

            // Fallbacks have their own callable IDs; bind each original external to its prepared target.
            foreach ((int originalId, IrFunctionId functionId) in fallbacks)
                calls[originalId] = fallbackCalls[functionId.Value];

            fallbackFunctions = prepared;
        }

        // Bind functions only after all native and fallback targets have been resolved.
        List<PreparedFunction> functions = new List<PreparedFunction>(callables.Count + fallbackFunctions.Count);

        for (int i = 0; i < callables.Count; i++)
        {
            if (callables[i] is IrFunction function)
                functions.Add(new PreparedFunction(calls[i].Name, function, calls));
        }

        functions.AddRange(fallbackFunctions);

        return (functions, calls, requiredBuiltins);
    }
}