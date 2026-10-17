using Genbox.FastData.Ir.Extensions;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Ir.Helpers;

/// <summary>Queries expression result types in validated IR for use by backends and analysis tools.</summary>
/// <remarks>
/// Resolves types using program declarations, function variables, and the IR operator rules.
/// Backends use the result to preserve IR semantics when target-language rules differ, such as integer promotion,
/// string ordering, or the choice of a bit-cast operation.
/// </remarks>
public static class IrExpressionTypes
{
    /// <summary>Returns the IR result type of expression in the supplied program and function context.</summary>
    /// <param name="program">The validated program containing the referenced data, fields, and callable declarations.</param>
    /// <param name="function">The function whose parameter and local declarations resolve variable references.</param>
    /// <param name="expression">An expression valid in that context.</param>
    /// <returns>The expression's result type according to the IR operator rules.</returns>
    /// <remarks>
    /// Literals and conversions carry their result types; declaration references to obtain theirs from the context.
    /// Indexing returns the collection's element type. Comparisons and logical expressions return Boolean;
    /// other binary operations retain the left operand's type.
    /// </remarks>
    /// <exception cref="NotSupportedException">The expression has an unsupported node type.</exception>
    public static IrType Of(IrProgram program, IrFunction function, IrExpression expression) => expression switch
    {
        IrLiteral literal => literal.Type,
        IrDataExpression data => program.Resolve(data.DataId).Type,
        IrVariableExpression variable => function.GetVariable(variable.VariableId).Type,
        IrFieldExpression field => program.Resolve(field.FieldId).Type,
        IrIndexExpression index => Of(program, function, index.Collection).ElementType!,
        IrBinaryExpression binary => binary.Operation.IsComparison() ? IrType.Bool : Of(program, function, binary.Left),
        IrLogicalExpression => IrType.Bool,
        IrConversionExpression conversion => conversion.TargetType,
        IrFunctionCallExpression call => program.ResolveCallable(call.FunctionId).Signature.ReturnType,
        _ => throw new NotSupportedException($"Unsupported IR expression '{expression.GetType().Name}'.")
    };
}