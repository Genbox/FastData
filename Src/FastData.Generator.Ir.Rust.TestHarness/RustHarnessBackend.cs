using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;
using static Genbox.FastData.Generator.Ir.Emission.SourceWriter;

namespace Genbox.FastData.Generator.Ir.Rust.TestHarness;

/// <summary>Provides the Rust toolchain and runtime operations for generated IR harnesses.</summary>
public sealed class RustHarnessBackend : HarnessBackend
{
    public override string Name => "Rust";
    public override string Image => "rust:1.96.0-bookworm@sha256:19817ead3289c8c631c73df281e18b59b172f6a31f4f563290f69cddd06c30e9";
    public override string Extension => ".rs";
    public override IrCodeGenerator Generator() => new RustIrCodeGenerator(new IrCodeGeneratorConfig("LookupTable"));

    // Tests keep overflow checks on so unintended wrapping panics; benchmarks build like release code.
    public override string BuildCommand(string id, bool benchmark) => $"rustc --edition=2021 -A warnings -C opt-level={(benchmark ? 3 : 0)} -C overflow-checks={(benchmark ? "no" : "yes")} {id}.rs -o {id}";

    public override string RunCommand(string id, string arguments) => $"./{id} {arguments}";

    // Stored text is optional; public keys take the underlying encoded reference.
    private static string Key(IrType keyType, string expression) => keyType == IrType.String ? $"({expression}).unwrap()" : expression;

    protected override string Wrap(string members, bool benchmark) =>
        $$"""
          struct Harness;
          impl Harness
          {
          {{Indent(members)}}
          }
          fn main()
          {
              {{(benchmark ? """
                             let args: Vec<String> = std::env::args().collect();
                             std::process::exit(Harness::Run(args[1].parse().unwrap(), args[2].parse().unwrap(), args[3].parse().unwrap()));
                             """ : "std::process::exit(Harness::Run());")}}
          }
          """;

    protected override string Operation(IrType keyType, HarnessOperation operation, ReadOnlySpan<string> arguments) => operation switch
    {
        HarnessOperation.Contains => $"LookupTable::contains({Key(keyType, arguments[0])})",
        HarnessOperation.LookupMatches => $"(LookupTable::try_lookup({Key(keyType, arguments[0])}) == Some({arguments[1]}))",
        HarnessOperation.LookupMiss => $"LookupTable::try_lookup({Key(keyType, arguments[0])}).is_none()",
        HarnessOperation.KeyCount => "(Self::actual_keys().len() as i32)",
        HarnessOperation.KeyAt => $"Self::actual_keys()[{arguments[0]} as usize]",
        HarnessOperation.ValueCount => "(Self::actual_values().len() as i32)",
        HarnessOperation.ValueAt => $"Self::actual_values()[{arguments[0]} as usize]",
        HarnessOperation.KeySpanCount => "(LookupTable::key_slice().len() as i32)",
        HarnessOperation.KeySpanAt => $"LookupTable::key_slice()[{arguments[0]} as usize]",
        HarnessOperation.ValueSpanCount => "(LookupTable::value_slice().len() as i32)",
        HarnessOperation.ValueSpanAt => $"LookupTable::value_slice()[{arguments[0]} as usize]",
        HarnessOperation.Clock => "Self::clock()",
        HarnessOperation.Elapsed => $"((Self::clock() - {arguments[0]}) as f64)",
        HarnessOperation.Opaque => $"std::hint::black_box({arguments[0]})",
        HarnessOperation.Consume => $"{{ std::hint::black_box({arguments[0]}); 0i64 }}",
        HarnessOperation.Report => $"Self::report({string.Join(", ", arguments)})",
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    protected override string ApiAdapters(IrSourceEmitter emitter)
    {
        FastDataProgram lookup = emitter.Lookup.Lookup;
        SourceWriter writer = new SourceWriter();

        string keyType = emitter.Type(lookup.KeyType);
        if (lookup.Contract.Enumeration == null)
            return writer.ToString();

        writer.Line(Snapshot("keys", keyType));

        if (lookup.ValueType is {} valueType)
            writer.Line(Snapshot("values", emitter.Type(valueType)));

        return writer.ToString();
    }

    // Collects an enumeration once so the checks can index it.
    private static string Snapshot(string name, string type) =>
        $$"""
          fn actual_{{name}}() -> &'static Vec<{{type}}>
          {
              static DATA: std::sync::OnceLock<Vec<{{type}}>> = std::sync::OnceLock::new();
              DATA.get_or_init(|| LookupTable::{{name}}().collect())
          }
          """;

    protected override string Runtime() =>
        """
        fn clock() -> i64
        {
            static START: std::sync::OnceLock<std::time::Instant> = std::sync::OnceLock::new();
            START.get_or_init(std::time::Instant::now).elapsed().as_nanos() as i64
        }

        fn report(overhead: f64, elapsed: f64, found: i64) -> i64
        {
            println!("overhead {}\nsample {} {}", overhead, elapsed, found);
            0
        }
        """;
}