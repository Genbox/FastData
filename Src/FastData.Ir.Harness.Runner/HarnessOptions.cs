using System.Globalization;

namespace Genbox.FastData.Ir.Harness.Runner;

internal sealed class HarnessOptions
{
    public string[] Languages { get; private set; } = [];
    public string Filter { get; private set; } = "*";
    public string Output { get; private set; } = Path.Combine(Path.GetTempPath(), "FastData", "IrHarness", Guid.NewGuid().ToString("N"));
    public bool EmitOnly { get; private set; }
    public bool List { get; private set; }
    public BenchmarkWorkload Workload { get; private set; } = BenchmarkWorkload.Mixed;
    public int Warmups { get; private set; } = 3;
    public int Samples { get; private set; } = 10;
    public int TargetMs { get; private set; } = 100;
    public long? Invocations { get; private set; }

    public static HarnessOptions Parse(string[] args)
    {
        HarnessOptions result = new HarnessOptions();

        for (int i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new FormatException("Missing option value.");
            int Positive() => int.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value > 0 ? value : throw new FormatException("Expected a positive integer.");
            long PositiveLong() => long.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out long value) && value > 0 ? value : throw new FormatException("Expected a positive integer.");

            switch (args[i])
            {
                case "--language": result.Languages = Value().Split(','); break;
                case "--filter": result.Filter = Value(); break;
                case "--output": result.Output = Path.GetFullPath(Value()); break;
                case "--workload": result.Workload = Enum.Parse<BenchmarkWorkload>(Value(), true); break;
                case "--warmups": result.Warmups = Positive(); break;
                case "--samples": result.Samples = Positive(); break;
                case "--target-ms": result.TargetMs = Positive(); break;
                case "--invocations": result.Invocations = PositiveLong(); break;
                case "--emit-only": result.EmitOnly = true; break;
                case "--list": result.List = true; break;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.", nameof(args));
            }
        }

        return result;
    }
}