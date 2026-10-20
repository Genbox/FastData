using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Generator.Ir.Validation;

/// <summary>Turns a validated generic program and a lookup contract into a <see cref="FastDataProgram" />.</summary>
public static class FastDataValidator
{
    /// <summary>Checks the contract and builtin declarations, throwing at the first invalid binding.</summary>
    /// <remarks>Finite stored floating-point keys, NaN-query misses, search result slots, successful-result nullability, and custom hash semantics remain producer assertions.</remarks>
    public static FastDataProgram Validate(IrProgram program, LookupContract contract)
    {
        IrDiagnosticPath contractPath = IrDiagnosticPath.Root("Contract");
        IrDiagnosticPath findPath = contractPath + ".FindFunctionId";
        IrFunction find = ResolveFunction(program, contract.FindFunctionId, findPath);
        Require(find.ReturnType == IrType.I32 && find.Parameters.Count == 1, "A search function takes one key and returns an I32 slot or -1 when absent.", findPath, IrDiagnosticCode.InvalidContract);

        IrType keyType = find.Parameters[0].Type;
        IrType? valueType = contract.Values is {} values ? ValidateValues(program, values, contractPath + ".Values") : null;

        if (contract.Enumeration is {} enumeration)
            ValidateEnumeration(program, enumeration, keyType, valueType, contractPath + ".Enumeration");

        for (int i = 0; i < contract.Constants.Count; i++)
        {
            IrDataId id = contract.Constants[i];
            IrDiagnosticPath path = contractPath.Index("Constants", i);
            Require(id.Type == program.Resolve(id, path).Type, "Constant reference type does not match its declaration.", path, IrDiagnosticCode.InvalidContract);
        }

        IrBuiltins.Validate(program);
        return new FastDataProgram(program, contract);
    }

    private static IrType ValidateValues(IrProgram program, LookupValues values, IrDiagnosticPath path)
    {
        IrData data = program.Resolve(values.DataId, path + ".DataId");
        Require(values.DataId.Type == data.Type, "Value storage reference type does not match its declaration.", path + ".DataId", IrDiagnosticCode.InvalidContract);
        Require(data.Type.Kind == IrTypeKind.Array && data.Value is not IrNullLiteral, "Value storage must be a non-null array.", path + ".DataId", IrDiagnosticCode.InvalidContract);
        IrType valueType = data.Type.ElementType!;

        if (values.FieldId is {} field)
        {
            Require(valueType.Kind == IrTypeKind.Record && valueType.RecordId == field.RecordId, "The value field must belong to the storage array's record type.", path + ".FieldId", IrDiagnosticCode.InvalidContract);
            valueType = program.Resolve(field, path + ".FieldId").Type;
            Require(field.Type == valueType, "Value field reference type does not match its declaration.", path + ".FieldId", IrDiagnosticCode.InvalidContract);
        }

        return valueType;
    }

    private static void ValidateEnumeration(IrProgram program, LookupEnumeration enumeration, IrType keyType, IrType? valueType, IrDiagnosticPath path)
    {
        ValidateCursorFunction(program, enumeration.NextFunctionId, IrType.I64, path + ".NextFunctionId");
        ValidateCursorFunction(program, enumeration.KeyFunctionId, keyType, path + ".KeyFunctionId");

        if (enumeration.ValueFunctionId is {} value)
        {
            IrDiagnosticPath valuePath = path + ".ValueFunctionId";
            Require(valueType != null, "Value enumeration requires value storage.", valuePath, IrDiagnosticCode.InvalidContract);
            ValidateCursorFunction(program, value, valueType!, valuePath);
        }
    }

    private static void ValidateCursorFunction(IrProgram program, IrFunctionId id, IrType resultType, IrDiagnosticPath path)
    {
        IrFunction function = ResolveFunction(program, id, path);
        Require(function.ReturnType == resultType && function.Parameters.Count == 1 && function.Parameters[0].Type == IrType.I64, "Enumeration requires Next(I64) -> I64, Key(I64) of the key type, and Value(I64) of the value type.", path, IrDiagnosticCode.InvalidContract);
    }

    private static IrFunction ResolveFunction(IrProgram program, IrFunctionId id, IrDiagnosticPath path)
    {
        IrFunction function = program.ResolveCallable(id, path) as IrFunction ?? throw Fail($"Lookup binding requires a defined function; ID {id.Value} refers to an external declaration.", path, IrDiagnosticCode.InvalidContract);
        Require(id.ReturnType == function.ReturnType, "Function reference type does not match its declaration.", path, IrDiagnosticCode.InvalidContract);
        return function;
    }
}