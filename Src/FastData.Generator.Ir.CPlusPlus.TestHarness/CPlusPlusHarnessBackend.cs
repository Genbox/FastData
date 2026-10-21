using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.CPlusPlus.TestHarness;

/// <summary>Provides the CPlusPlus toolchain and runtime operations for generated IR harnesses.</summary>
public sealed class CPlusPlusHarnessBackend : HarnessBackend
{
    public override string Name => "CPlusPlus";
    public override string Image => "silkeh/clang:21-bookworm@sha256:c735c54928e1b8be46101c9aff2895f72d1bfb6d94f713804528fa2fd0a96dba";
    public override string Extension => ".cpp";
    public override IrCodeGenerator Generator() => new CPlusPlusIrCodeGenerator(new IrCodeGeneratorConfig("LookupTable"));

    // Tests abort on the first undefined behavior; benchmarks build like release code.
    public override string BuildCommand(string id, bool benchmark)
    {
        string flags = benchmark ? "-O3 -DNDEBUG" : "-O0 -fsanitize=undefined -fno-sanitize-recover=all";
        return $"clang++ -std=c++20 {flags} {id}.cpp -o {id}";
    }

    public override string RunCommand(string id, string arguments) => $"./{id} {arguments}";

    // Stored text is optional; public keys take the underlying encoded view.
    private static string Key(IrType keyType, string expression) => keyType == IrType.String ? $"*({expression})" : expression;

    protected override string Wrap(string members, bool benchmark) =>
        $$"""
          #include <vector>
          #include <chrono>
          #include <iostream>
          #include <iomanip>
          #include <limits>
          #include <locale>
          #include <string>

          {{members}}

          int main(int argc, char** argv)
          {
              {{(benchmark ? """
                             std::cout.imbue(std::locale::classic());
                             return Run(std::stoll(argv[1]), std::stoll(argv[2]), std::stoll(argv[3]));
                             """ : "return Run();")}}
          }
          """;

    protected override string Operation(IrType keyType, HarnessOperation operation, ReadOnlySpan<string> arguments) => operation switch
    {
        HarnessOperation.Contains => $"LookupTable::Contains({Key(keyType, arguments[0])})",
        HarnessOperation.LookupMatches => $"Matches({arguments[0]}, {arguments[1]})",
        HarnessOperation.LookupMiss => $"Miss({arguments[0]})",
        HarnessOperation.KeyCount => "static_cast<std::int32_t>(actualKeys().size())",
        HarnessOperation.KeyAt => $"actualKeys()[{arguments[0]}]",
        HarnessOperation.ValueCount => "static_cast<std::int32_t>(actualValues().size())",
        HarnessOperation.ValueAt => $"actualValues()[{arguments[0]}]",
        HarnessOperation.KeySpanCount => "static_cast<std::int32_t>(LookupTable::KeySpan().size())",
        HarnessOperation.KeySpanAt => $"LookupTable::KeySpan()[{arguments[0]}]",
        HarnessOperation.ValueSpanCount => "static_cast<std::int32_t>(LookupTable::ValueSpan().size())",
        HarnessOperation.ValueSpanAt => $"LookupTable::ValueSpan()[{arguments[0]}]",
        HarnessOperation.Clock => "Clock()",
        HarnessOperation.Elapsed => $"static_cast<double>(Clock() - {arguments[0]})",
        HarnessOperation.Opaque => $"Opaque({arguments[0]})",
        HarnessOperation.Consume => $"Consume({arguments[0]})",
        HarnessOperation.Report => $"Report({string.Join(", ", arguments)})",
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    protected override string ApiAdapters(IrSourceEmitter emitter)
    {
        FastDataProgram lookup = emitter.Lookup.Lookup;
        SourceWriter writer = new SourceWriter();
        string keyType = emitter.Type(lookup.KeyType);
        bool keyed = lookup.Contract.Values != null;

        if (keyed)
        {
            string valueType = emitter.Type(lookup.ValueType!);
            string equals = lookup.ValueType == IrType.String ? new CPlusPlusText(emitter.Lookup.Encoding).Comparison(BinaryOperation.Equal, "value", "expected") : "value == expected";

            writer.Line($$"""
                          static bool Matches({{keyType}} key, {{valueType}} expected)
                          {
                              {{valueType}} value{};
                              return LookupTable::TryLookup({{Key(lookup.KeyType, "key")}}, value) && {{equals}};
                          }
                          static bool Miss({{keyType}} key)
                          {
                              {{valueType}} value{};
                              return !LookupTable::TryLookup({{Key(lookup.KeyType, "key")}}, value);
                          }
                          """);
        }

        if (lookup.Contract.Enumeration == null)
            return writer.ToString();

        writer.Line(Snapshot("Keys", keyType));

        if (keyed)
            writer.Line(Snapshot("Values", emitter.Type(lookup.ValueType!)));

        return writer.ToString();
    }

    // Collects an enumeration once so the checks can index it.
    private static string Snapshot(string name, string type) =>
        $$"""
          static const std::vector<{{type}}>& actual{{name}}()
          {
              static const auto data = []
              {
                  std::vector<{{type}}> items;
                  for (auto value : LookupTable::{{name}}())
                      items.push_back(value);
                  return items;
              }();
              return data;
          }
          """;

    // The empty asm statements are optimizer barriers: Opaque hides a value's origin and Consume forces it to be materialized.
    protected override string Runtime() =>
        """
        template<class T> static T Opaque(T value)
        {
            asm volatile("" : "+m"(value) : : "memory");
            return value;
        }

        template<class T> static std::int64_t Consume(T value)
        {
            asm volatile("" : : "m"(value) : "memory");
            return 0;
        }

        static std::int64_t Clock()
        {
            return std::chrono::duration_cast<std::chrono::nanoseconds>(std::chrono::steady_clock::now().time_since_epoch()).count();
        }

        static std::int64_t Report(double overhead, double elapsed, std::int64_t found)
        {
            std::cout << std::setprecision(std::numeric_limits<double>::max_digits10)
                      << "overhead " << overhead << '\n'
                      << "sample " << elapsed << ' ' << found << '\n';
            return 0;
        }
        """;
}