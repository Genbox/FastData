using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Harness;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Model;
using static Genbox.FastData.Generator.Ir.Emission.SourceWriter;

namespace Genbox.FastData.Generator.Ir.CSharp.TestHarness;

/// <summary>Provides the CSharp toolchain and runtime operations for generated IR harnesses.</summary>
public sealed class CSharpHarnessBackend : HarnessBackend
{
    // Versions installed in the pinned SDK image. Compiling with csc directly avoids a project restore per case.
    private const string RuntimeVersion = "10.0.9";
    private const string SdkVersion = "10.0.301";

    public override string Name => "CSharp";
    public override string Image => "mcr.microsoft.com/dotnet/sdk:10@sha256:548d93f8a18a1acbe6cc127bc4f47281430d34a9e35c18afa80a8d6741c2adc3";
    public override string Extension => ".cs";
    public override IrCodeGenerator Generator() => new CSharpIrCodeGenerator(new CSharpIrCodeGeneratorConfig("LookupTable"));

    public override string BuildCommand(string id, bool benchmark) => $"""
                                                                       find /usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/{RuntimeVersion}/ref/net10.0 -name '*.dll' -printf '-r:%p\n' > refs.rsp
                                                                       dotnet /usr/share/dotnet/sdk/{SdkVersion}/Roslyn/bincore/csc.dll \
                                                                           -nologo -noconfig -nostdlib+ -nullable:enable -langversion:latest \
                                                                           -optimize{(benchmark ? "+" : "-")} -target:exe -out:{id}.dll @refs.rsp {id}.cs
                                                                       """;

    // The compiler's own runtime config is reused, so no per-case runtimeconfig.json is needed.
    public override string RunCommand(string id, string arguments) => $"""
                                                                       DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 \
                                                                           dotnet exec --runtimeconfig /usr/share/dotnet/sdk/{SdkVersion}/Roslyn/bincore/csc.runtimeconfig.json \
                                                                           {id}.dll {arguments}
                                                                       """;

    protected override string Wrap(string members, bool benchmark) =>
        $$"""
          internal static class Harness
          {
          {{Indent(members)}}
              public static int Main(string[] args) => {{(benchmark ? "Run(long.Parse(args[0]), long.Parse(args[1]), long.Parse(args[2]))" : "Run()")}};
          }
          """;

    protected override string Operation(IrType keyType, HarnessOperation operation, ReadOnlySpan<string> arguments) => operation switch
    {
        HarnessOperation.Contains => $"LookupTable.Contains({arguments[0]})",
        HarnessOperation.LookupMatches => $"Matches({arguments[0]}, {arguments[1]})",
        HarnessOperation.LookupMiss => $"!LookupTable.TryLookup({arguments[0]}, out _)",
        HarnessOperation.KeyCount => "actualKeys.Length",
        HarnessOperation.KeyAt => $"actualKeys[{arguments[0]}]",
        HarnessOperation.ValueCount => "actualValues.Length",
        HarnessOperation.ValueAt => $"actualValues[{arguments[0]}]",
        HarnessOperation.KeySpanCount => "LookupTable.KeySpan.Length",
        HarnessOperation.KeySpanAt => $"LookupTable.KeySpan[{arguments[0]}]",
        HarnessOperation.ValueSpanCount => "LookupTable.ValueSpan.Length",
        HarnessOperation.ValueSpanAt => $"LookupTable.ValueSpan[{arguments[0]}]",
        HarnessOperation.Clock => "System.Diagnostics.Stopwatch.GetTimestamp()",
        HarnessOperation.Elapsed => $"((System.Diagnostics.Stopwatch.GetTimestamp() - {arguments[0]}) * (1000000000.0 / System.Diagnostics.Stopwatch.Frequency))",
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

        // IEEE equality, matching the C++ and Rust adapters.
        if (keyed)
        {
            string valueType = emitter.Type(lookup.ValueType!);
            string equals = lookup.ValueType == IrType.String ? new CSharpText(emitter.Lookup.Encoding).Equal("value", "expected") : "value == expected";

            writer.Line($"""
                         private static bool Matches({keyType} key, {valueType} expected) =>
                             LookupTable.TryLookup(key, out {valueType} value) && {equals};
                         """);
        }

        if (lookup.Contract.Enumeration == null)
            return writer.ToString();

        // Enumerations are snapshotted once so the checks can index them.
        writer.Line($"private static readonly {keyType}[] actualKeys = System.Linq.Enumerable.ToArray(LookupTable.Keys);");

        if (keyed)
            writer.Line($"private static readonly {emitter.Type(lookup.ValueType!)}[] actualValues = System.Linq.Enumerable.ToArray(LookupTable.Values);");

        return writer.ToString();
    }

    protected override string Runtime() =>
        """
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        private static T Opaque<T>(T value) => value;

        private static long Consume<T>(T value)
        {
            System.GC.KeepAlive(value);
            return 0L;
        }

        private static long Report(double overhead, double elapsed, long found)
        {
            System.Console.WriteLine("overhead " + overhead.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            System.Console.WriteLine("sample " + elapsed.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " " + found.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0L;
        }
        """;
}