using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Extensions;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Generator.Ir.Emission.SourceWriter;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Renders original IR using preparation metadata and manages source emission for a lookup and its companion programs.</summary>
public abstract class IrSourceEmitter(EmissionContext context, LiteralFormatter literals)
{
    public PreparedLookup Lookup => context.Lookup;

    private PreparedEmissionPlan Plan => context.Plan;
    protected virtual string ApiKey => "key";

    /// <summary>Creates an independent renderer with its own emission plan; custom expressions receive their rendered operands.</summary>
    public IrSourceEmitter CreateCompanion(PreparedEmissionPlan plan, Func<IrExpression, string[], string> customExpression) => CreateEmitter(new EmissionContext(Lookup, plan, customExpression));

    protected abstract IrSourceEmitter CreateEmitter(EmissionContext context);

    public string Type(IrType type) => literals.Type(type);
    public string Literal(IrLiteral literal) => literals.Literal(literal);
    protected string Initializer(IrLiteral literal) => literals.Initializer(literal);

    /// <summary>Renders aggregate storage in dependency order at the requested indentation.</summary>
    public string DataDeclarations(int indent = 0) => JoinLines(Plan.Data.Select(Data), indent);

    /// <summary>Renders prepared functions at the requested indentation.</summary>
    public string FunctionDeclarations(int indent = 0) => JoinLines(Plan.Functions.Select(RenderFunction), indent);

    private string RenderFunction(PreparedFunction function)
    {
        IrFunction definition = function.Function;
        string parameters = string.Join(", ", definition.Parameters.Select((parameter, i) => Parameter(function.VariableNames[i], parameter.Type)));
        SourceWriter writer = new SourceWriter();

        for (int i = 0; i < definition.Locals.Count; i++)
            writer.Line(Local(function.VariableNames[definition.Parameters.Count + i], definition.Locals[i].Type));

        WriteStatements(writer, definition.Body, function.VariableNames, BindExpressions(function));
        return Function(function.Name, definition.ReturnType, parameters, writer.ToString());
    }

    /// <summary>Renders a block in source order. Backends may override this to select native control-flow constructs.</summary>
    protected virtual void WriteStatements(SourceWriter writer, IrBlock block, IReadOnlyList<string> variables, Func<IrExpression, string> expression)
    {
        foreach (IrStatement statement in block.Statements)
            WriteStatement(writer, statement, variables, expression);
    }

    /// <summary>Renders an original IR statement without control-flow optimization.</summary>
    protected void WriteStatement(SourceWriter writer, IrStatement statement, IReadOnlyList<string> variables, Func<IrExpression, string> expression)
    {
        switch (statement)
        {
            case IrAssignmentStatement assignment:
                writer.Line($"{variables[assignment.VariableId.Value]} = {expression(assignment.Value)};");
                break;
            case IrReturnStatement result:
                writer.Line($"return {expression(result.Value)};");
                break;
            case IrBreakStatement:
                writer.Line("break;");
                break;
            case IrIfStatement conditional:
                writer.Line($"if ({expression(conditional.Condition)})");
                WriteBlock(writer, conditional.IfTrue, variables, expression);

                if (conditional.IfFalse is {} otherwise)
                {
                    writer.Line("else");
                    WriteBlock(writer, otherwise, variables, expression);
                }

                break;
            case IrLoopStatement loop:
                writer.Line($"while ({expression(loop.Condition)})");
                WriteBlock(writer, loop.Body, variables, expression);
                break;
            default:
                throw new NotSupportedException($"Statement '{statement.GetType().Name}' is not supported by emission.");
        }
    }

    private void WriteBlock(SourceWriter writer, IrBlock block, IReadOnlyList<string> variables, Func<IrExpression, string> expression)
    {
        writer.Open();
        WriteStatements(writer, block, variables, expression);
        writer.Close();
    }

    /// <summary>Formats packed initializer elements in indented rows.</summary>
    protected string Elements(IrArrayLiteralBase array) => FormatElements(literals.Elements(array), array.Count, array.ElementType.Kind == IrTypeKind.Record ? 1 : 8);

    /// <summary>Renders the available public members in lookup API order.</summary>
    protected string PublicApi()
    {
        LookupContract contract = Lookup.Lookup.Contract;
        string find = $"{MemberReference(Lookup.FunctionName(contract.FindFunctionId))}({ApiKey})";
        string zero = Literal(I32(0));
        RenderedLookupApi api = new RenderedLookupApi(PublicKeyType(Lookup.Lookup.KeyType), $"({find} >= {zero})", find, $"(slot >= {zero})");
        SourceWriter writer = new SourceWriter();
        writer.Line(Contains(api));
        if (contract.Values is {} value)
            writer.Line(TryLookup(api, RenderValue(value)));
        if (Lookup.KeyStorage is {} keys)
            writer.Line(Slice(true, RenderSlice(keys)));
        if (Lookup.ValueStorage is {} values)
            writer.Line(Slice(false, RenderSlice(values)));

        if (contract.Enumeration is {} enumeration)
        {
            writer.Line(RenderEnumeration("Keys", enumeration.NextFunctionId, enumeration.KeyFunctionId));
            if (enumeration.ValueFunctionId is {} element)
                writer.Line(RenderEnumeration("Values", enumeration.NextFunctionId, element));
        }

        return writer.ToString();

        string RenderEnumeration(string name, IrFunctionId nextId, IrFunctionId elementId)
        {
            string initial = Literal(I64(-1));
            string next = MemberReference(Lookup.FunctionName(nextId));
            string element = MemberReference(Lookup.FunctionName(elementId));
            return Enumeration(new RenderedEnumeration(name, Type(elementId.ReturnType), initial,
                $"{next}({initial})", $"{next}(position)", $"(position >= {Literal(I64(0))})", $"{element}(position)"));
        }

        RenderedLookupValue RenderValue(LookupValues val)
        {
            string expression = IndexAccess(MemberReference(Lookup.DataName(val.DataId)), "slot");

            if (val.FieldId is {} field)
                expression = FieldAccess(expression, field);

            return new RenderedLookupValue(Type(val.Type), expression);
        }

        RenderedLookupValue RenderSlice(IrDataId storage) => new RenderedLookupValue(Type(storage.Type.ElementType!), MemberReference(Lookup.DataName(storage)));
    }

    protected virtual string PublicKeyType(IrType type) => Type(type);
    protected abstract string Contains(RenderedLookupApi api);
    protected abstract string TryLookup(RenderedLookupApi api, RenderedLookupValue value);
    protected abstract string Slice(bool keys, RenderedLookupValue slice);
    protected abstract string Enumeration(RenderedEnumeration enumeration);

    /// <summary>Renders the lookup source.</summary>
    public string Emit() => JoinLines([EmitSource()]);

    /// <summary>Binds a renderer once for a scope, including all recursive operands.</summary>
    private Func<IrExpression, string> BindExpressions(PreparedFunction function)
    {
        return Render;

        string Render(IrExpression expression) => expression switch
        {
            IrLiteral literal => Literal(literal),
            IrVariableExpression variable => function.VariableNames[variable.VariableId.Value],
            IrDataExpression data => MemberReference(Lookup.DataName(data.DataId)),
            IrFieldExpression field => FieldAccess(Render(field.Receiver), field.FieldId),
            IrIndexExpression index => IndexAccess(Render(index.Collection), Render(index.Index)),
            IrLogicalExpression logical => $"({Render(logical.Left)} {CStyleOperators.Symbol(logical.Operation)} {Render(logical.Right)})",
            IrBinaryExpression { Operation: BinaryOperation.Equal or BinaryOperation.NotEqual } binary when binary.Left is IrNullLiteral || binary.Right is IrNullLiteral => NullCheck(Render(binary.Left is IrNullLiteral ? binary.Right : binary.Left), binary.Operation == BinaryOperation.Equal),
            IrBinaryExpression binary when binary.Operation.IsComparison() && binary.Left.Type != IrType.String => $"({Render(binary.Left)} {CStyleOperators.Symbol(binary.Operation)} {Render(binary.Right)})",
            IrFunctionCallExpression call => RenderCall(call),
            IrExtensionExpression extension => context.CustomExpression?.Invoke(expression, Arguments(extension.Children, Render)) ?? throw new NotSupportedException($"Unsupported expression '{expression.GetType().Name}'."),
            _ => FormatExpression(expression, Render)
        };

        string RenderCall(IrFunctionCallExpression call)
        {
            PreparedCall target = function.Calls[call.FunctionId.Value];
            string[] arguments = Arguments(call.Arguments, Render);
            return target.Operation == IrBuiltinOperation.None ? $"{MemberReference(target.Name)}({string.Join(", ", arguments)})" : Builtin(target.Operation, arguments);
        }
    }

    private static string[] Arguments(IReadOnlyList<IrExpression> arguments, Func<IrExpression, string> render)
    {
        string[] result = new string[arguments.Count];
        for (int i = 0; i < result.Length; i++)
            result[i] = render(arguments[i]);
        return result;
    }

    /// <summary>Qualifies a member name for the current emitted scope.</summary>
    public virtual string MemberReference(string name) => name;

    protected abstract string FormatExpression(IrExpression expression, Func<IrExpression, string> render);
    protected abstract string IndexAccess(string collection, string index);
    protected abstract string FieldAccess(string receiver, IrFieldId field);
    protected abstract string NullCheck(string operand, bool isNull);
    protected abstract string Builtin(IrBuiltinOperation operation, string[] arguments);
    protected abstract string Data(PreparedData data);
    protected abstract string EmitSource();
    protected virtual string Parameter(string name, IrType type) => $"{Type(type)} {name}";
    protected abstract string Local(string name, IrType type);
    protected abstract string Function(string name, IrType returnType, string parameters, string body);
}