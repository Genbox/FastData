using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Generator.Ir.Emission.SourceWriter;

namespace Genbox.FastData.Generator.Ir.CPlusPlus.Internal;

internal sealed class CPlusPlusEmitter(IrCodeGeneratorConfig config, EmissionContext context) : IrSourceEmitter(context, new CPlusPlusLiterals(context.Lookup.Records, context.Lookup.Encoding, context.Plan.Reference))
{
    private readonly CPlusPlusText _text = new CPlusPlusText(context.Lookup.Encoding);

    protected override IrSourceEmitter CreateEmitter(EmissionContext context) => new CPlusPlusEmitter(config, context);

    protected override string EmitSource() =>
        $$"""
          // Generated from the FastData portable IR.
          #include <algorithm>
          #include <array>
          #include <compare>
          #include <bit>
          #include <cstdint>
          #include <optional>
          #include <span>
          #include <string_view>

          class {{config.ClassName}}
          {
              public:
          {{JoinLines(Lookup.Records.Select(record => $"struct {record.Name};"), 1)}}
          {{JoinLines(Lookup.Records.Select(Record), 1)}}
              private:
          {{DataDeclarations(1)}}
          {{FunctionDeclarations(1)}}
              public:
          {{Indent(PublicApi())}}
          };
          """;

    private string Record(PreparedRecord record) =>
        $$"""
          struct {{record.Name}}
          {
          {{JoinLines(record.Record.Fields.Select((field, i) => $"{Type(field.Type)} {record.FieldNames[i]};"), 1)}}
          };
          """;

    protected override string Data(PreparedData data)
    {
        (string storage, string initializer) = data.Value switch
        {
            IrArrayLiteralBase array => ($$"""
                                           inline static const std::array<{{Type(array.ElementType)}}, {{array.Count}}> {{data.Name}}Storage =
                                           {
                                           {{Elements(array)}}
                                           };
                                           """, $"std::span{{{data.Name}Storage}}"),
            IrRecordLiteral record => ($"inline static const {Lookup.Records[record.RecordId.Value].Name} {data.Name}Storage = {Initializer(record)};", $"&{data.Name}Storage"),
            _ => ("", Initializer(data.Value))
        };
        string declaration = $"inline static {Type(data.Type)} const {data.Name} = {initializer};";

        // Backing storage stays private; only the named value can be exported.
        if (data.Exported)
        {
            declaration = $"""
                           public:
                           {declaration}
                           private:
                           """;
        }

        return storage.Length == 0
            ? declaration
            : $"""
               {storage}
               {declaration}
               """;
    }

    protected override string Local(string name, IrType type) => $"{Type(type)} {name}{{}};";

    protected override string Function(string name, IrType returnType, string parameters, string body) =>
        $$"""
          static {{Type(returnType)}} {{name}}({{parameters}})
          {
          {{Indent(body)}}
          }
          """;

    protected override string PublicKeyType(IrType type) => type == IrType.String ? _text.Type : Type(type);

    protected override string Contains(RenderedLookupApi api) => $"static bool Contains({api.KeyType} key) {{ return {api.Contains}; }}";

    protected override string TryLookup(RenderedLookupApi api, RenderedLookupValue value) =>
        $$"""
          static bool TryLookup({{api.KeyType}} key, {{value.Type}}& value)
          {
              auto slot = {{api.Find}};
              if ({{api.Found}})
              {
                  value = {{value.Expression}};
                  return true;
              }
              value = {};
              return false;
          }
          """;

    protected override string Slice(bool keys, RenderedLookupValue slice) => $"static auto {(keys ? "KeySpan" : "ValueSpan")}() {{ return *({slice.Expression}); }}";

    protected override string Enumeration(RenderedEnumeration enumeration) =>
        $$"""
          struct {{enumeration.Name + "Range"}}
          {
              struct Sentinel {};
              struct Iterator
              {
                  std::int64_t position;
                  auto operator*() const { return {{enumeration.Current}}; }
                  Iterator& operator++() { position = {{enumeration.Advance}}; return *this; }
                  bool operator!=(Sentinel) const { return {{enumeration.HasCurrent}}; }
              };
              Iterator begin() const { return { {{enumeration.Start}} }; }
              Sentinel end() const { return {}; }
          };
          static {{enumeration.Name + "Range"}} {{enumeration.Name}}() { return {}; }
          """;

    protected override void WriteStatements(SourceWriter writer, IrBlock block, IReadOnlyList<string> variables, Func<IrExpression, string> expression)
    {
        SwitchDetectionVisitor dispatch = new SwitchDetectionVisitor(variables);

        for (int i = 0; i < block.Statements.Count; i++)
        {
            dispatch.Visit(block, i);

            if (dispatch.VariableName is not {} name || dispatch.Type == IrType.String)
            {
                WriteStatement(writer, block.Statements[i], variables, expression);
                continue;
            }

            writer.Line($"switch ({name})");
            writer.Open();
            foreach ((IrLiteral label, IrExpression result) in dispatch.Cases)
                writer.Line($"case {expression(label)}: return {expression(result)};");
            writer.Close();
            i += dispatch.Cases.Count - 1;
        }
    }

    protected override string IndexAccess(string collection, string index) => $"(*({collection}))[{index}]";
    protected override string FieldAccess(string receiver, IrFieldId field) => $"({receiver})->{Lookup.FieldName(field)}";
    protected override string NullCheck(string operand, bool isNull) => $"({(isNull ? "!" : "")}static_cast<bool>({operand}))";
    protected override string Builtin(IrBuiltinOperation operation, string[] arguments) => _text.Call(operation, arguments);

    protected override string FormatExpression(IrExpression expression, Func<IrExpression, string> render) => expression switch
    {
        IrConversionExpression { Conversion: ConversionKind.BitCast } cast => $"std::bit_cast<{Type(cast.Type)}>({render(cast.Operand)})",
        IrConversionExpression conversion => $"static_cast<{Type(conversion.Type)}>({render(conversion.Operand)})",
        IrBinaryExpression binary when binary.Left.Type != IrType.String => Arithmetic(binary, render),
        IrBinaryExpression comparison => _text.Comparison(comparison.Operation, render(comparison.Left), render(comparison.Right)),
        _ => throw new NotSupportedException($"Unsupported expression '{expression.GetType().Name}'.")
    };

    private string Arithmetic(IrBinaryExpression arithmetic, Func<IrExpression, string> render)
    {
        IrType type = arithmetic.Left.Type;
        string left = render(arithmetic.Left);
        string right = render(arithmetic.Right);

        // uint16_t multiplication promotes to signed int and can overflow before narrowing.
        if (type == IrType.U16 && arithmetic.Operation == BinaryOperation.Multiply)
        {
            left = $"static_cast<std::uint64_t>({left})";
            right = $"static_cast<std::uint64_t>({right})";
        }

        string expression = $"({left} {CStyleOperators.Symbol(arithmetic.Operation)} {right})";
        return type is { IsIntegral: true, BitWidth: < 32 }
            ? $"static_cast<{Type(type)}>({expression})"
            : expression;
    }
}