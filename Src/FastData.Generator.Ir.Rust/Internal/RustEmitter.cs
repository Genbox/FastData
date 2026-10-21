using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Generator.Ir.Emission.SourceWriter;

namespace Genbox.FastData.Generator.Ir.Rust.Internal;

internal sealed class RustEmitter(IrCodeGeneratorConfig config, EmissionContext context) : IrSourceEmitter(context, new RustLiterals(context.Lookup.Records, context.Lookup.Encoding, config.ClassName + "_", literal => context.Plan.Reference(literal) is {} name ? "Self::" + name : null))
{
    private readonly RustText _text = new RustText(context.Lookup.Encoding);
    private readonly RustTypes _types = new RustTypes(context.Lookup.Records, context.Lookup.Encoding, config.ClassName + "_");

    protected override string ApiKey => Lookup.Lookup.KeyType == IrType.String ? "Some(key)" : "key";

    public override string MemberReference(string name) => "Self::" + name;

    protected override IrSourceEmitter CreateEmitter(EmissionContext context) => new RustEmitter(config, context);

    protected override string EmitSource() =>
        $$"""
          // Generated from the FastData portable IR.
          // Structure: {{Lookup.StructureName}}

          {{JoinLines(Lookup.Records.Select(Record))}}

          pub struct {{config.ClassName}};

          #[allow(non_snake_case, non_upper_case_globals, unused_mut, unused_assignments, unused_parens, dead_code)]
          impl {{config.ClassName}}
          {
          {{DataDeclarations(1)}}
          {{Indent(PublicApi())}}
          {{FunctionDeclarations(1)}}
          }
          """;

    private string Record(PreparedRecord record, int index) =>
        $$"""
          #[derive(Clone, Copy, PartialEq)]
          #[allow(non_camel_case_types, non_snake_case, dead_code)]
          struct {{_types.RecordName(index)}}<'a>
          {
          {{JoinLines(record.Record.Fields.Select((field, i) => $"{record.FieldNames[i]}: {_types.Type(field.Type, "'a")},"), 1)}}
              marker: core::marker::PhantomData<&'a ()>,
          }
          """;

    protected override string Data(PreparedData data)
    {
        string visibility = data.Exported ? "pub " : "";
        string declaration = $"{visibility}const {data.Name}: {Type(data.Type)}";

        if (data.Value is IrArrayLiteralBase { Count: > 8 } array)
        {
            return $"""
                    {declaration} = Some(&[
                    {Elements(array)}
                    ]);
                    """;
        }

        return $"{declaration} = {Initializer(data.Value)};";
    }

    protected override string Parameter(string name, IrType type) => $"mut {name}: {_types.Type(type, "'a")}";

    protected override string Local(string name, IrType type) => $"let mut {name}: {_types.Type(type, "'a")} = Default::default();";

    protected override string Function(string name, IrType returnType, string parameters, string body) =>
        $$"""
          #[inline(always)]
          fn {{name}}<'a>({{parameters}}) -> {{_types.Type(returnType, "'a")}}
          {
          {{Indent(body)}}
          }
          """;

    protected override void WriteStatements(SourceWriter writer, IrBlock block, IReadOnlyList<string> variables, Func<IrExpression, string> expression)
    {
        SwitchDetectionVisitor dispatch = new SwitchDetectionVisitor(variables);

        for (int i = 0; i < block.Statements.Count; i++)
        {
            dispatch.Visit(block, i);

            if (dispatch.VariableName is {} name)
            {
                writer.Line($"match {name}");
                writer.Open();

                foreach ((IrLiteral label, IrExpression result) in dispatch.Cases)
                    writer.Line($"{expression(label)} => return {expression(result)},");

                writer.Line("_ => {},");
                writer.Close();
                i += dispatch.Cases.Count - 1;
            }
            else
                WriteStatement(writer, block.Statements[i], variables, expression);
        }
    }

    protected override string PublicKeyType(IrType type) => type == IrType.String ? "&" + _text.Type : Type(type);

    protected override string Contains(RenderedLookupApi api) =>
        $$"""
          #[inline(always)]
          pub fn contains(key: {{api.KeyType}}) -> bool { {{api.Contains}} }
          """;

    protected override string TryLookup(RenderedLookupApi api, RenderedLookupValue value) =>
        $$"""
          #[inline(always)]
          pub fn try_lookup(key: {{api.KeyType}}) -> Option<{{value.Type}}>
          {
              let slot = {{api.Find}};
              if {{api.Found}} { Some({{value.Expression}}) } else { None }
          }
          """;

    protected override string Slice(bool keys, RenderedLookupValue slice) => $"pub fn {(keys ? "key_slice" : "value_slice")}() -> &'static [{slice.Type}] {{ {slice.Expression}.unwrap() }}";

    protected override string Enumeration(RenderedEnumeration enumeration) =>
        $$"""
          pub fn {{enumeration.Name.ToLowerInvariant()}}() -> impl Iterator<Item = {{enumeration.Type}}>
          {
              let mut position = {{enumeration.InitialPosition}};
              core::iter::from_fn(move || {
                  position = {{enumeration.Advance}};
                  if {{enumeration.HasCurrent}} { Some({{enumeration.Current}}) } else { None }
              }).fuse()
          }
          """;

    protected override string IndexAccess(string collection, string index) => $"({collection}).unwrap()[({index}) as usize]";
    protected override string FieldAccess(string receiver, IrFieldId field) => $"({receiver}).unwrap().{Lookup.FieldName(field)}";
    protected override string NullCheck(string operand, bool isNull) => $"({operand}).{(isNull ? "is_none" : "is_some")}()";
    protected override string Builtin(IrBuiltinOperation operation, string[] arguments) => RustRuntime.Call(operation, arguments, _text);

    protected override string FormatExpression(IrExpression expression, Func<IrExpression, string> render) => expression switch
    {
        IrConversionExpression { Conversion: ConversionKind.BitCast } cast => RustRuntime.BitCast(cast.Operand.Type, cast.Type, render(cast.Operand)),
        IrConversionExpression conversion => $"({render(conversion.Operand)} as {Type(conversion.Type)})",
        IrBinaryExpression binary when binary.Left.Type != IrType.String => Arithmetic(binary, render),
        IrBinaryExpression comparison => $"({render(comparison.Left)} {CStyleOperators.Symbol(comparison.Operation)} {render(comparison.Right)})",
        _ => throw new NotSupportedException($"Unsupported expression '{expression.GetType().Name}'.")
    };

    private static string Arithmetic(IrBinaryExpression arithmetic, Func<IrExpression, string> render)
    {
        if (arithmetic.Left.Type.IsUnsignedIntegral && arithmetic.Operation is BinaryOperation.Add or BinaryOperation.Subtract or BinaryOperation.Multiply)
            return $"({render(arithmetic.Left)}).{WrappingMethod(arithmetic.Operation)}({render(arithmetic.Right)})";

        return $"({render(arithmetic.Left)} {CStyleOperators.Symbol(arithmetic.Operation)} {render(arithmetic.Right)})";
    }

    private static string WrappingMethod(BinaryOperation operation) => operation switch
    {
        BinaryOperation.Add => "wrapping_add",
        BinaryOperation.Subtract => "wrapping_sub",
        BinaryOperation.Multiply => "wrapping_mul",
        _ => throw new NotSupportedException($"Unsupported wrapping operation '{operation}'.")
    };
}