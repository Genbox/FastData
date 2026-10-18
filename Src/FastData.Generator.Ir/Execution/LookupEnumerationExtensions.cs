using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Execution;

/// <summary>Evaluates lookup enumeration through the reference interpreter.</summary>
public static class LookupEnumerationExtensions
{
    /// <summary>Evaluates one key at a time through the reference interpreter.</summary>
    public static IEnumerable<IrLiteral> EnumerateKeys(this LookupEnumeration enumeration, IrInterpreter interpreter) => EnumerateCore(enumeration, interpreter, enumeration.KeyFunctionId);

    /// <summary>Evaluates one value at a time through the reference interpreter.</summary>
    /// <exception cref="InvalidOperationException">The enumeration supports keys alone.</exception>
    public static IEnumerable<IrLiteral> EnumerateValues(this LookupEnumeration enumeration, IrInterpreter interpreter)
    {
        IrFunctionId values = enumeration.ValueFunctionId ?? throw new InvalidOperationException("The enumeration has no value function.");
        return EnumerateCore(enumeration, interpreter, values);
    }

    private static IEnumerable<IrLiteral> EnumerateCore(LookupEnumeration enumeration, IrInterpreter interpreter, IrFunctionId elementFunctionId)
    {
        long position = Next(enumeration, interpreter, -1);

        while (position != -1)
        {
            yield return interpreter.Invoke(elementFunctionId, new IrI64Literal(position));
            position = Next(enumeration, interpreter, position);
        }
    }

    private static long Next(LookupEnumeration enumeration, IrInterpreter interpreter, long position) => ((IrI64Literal)interpreter.Invoke(enumeration.NextFunctionId, new IrI64Literal(position))).Value;
}