using System.Runtime.InteropServices;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal sealed class RunEnvironment : IDisposable
{
    private readonly WindowsPowerPlanState? _powerPlanState;

    private bool _disposed;

    private RunEnvironment(WindowsPowerPlanState? powerPlanState)
    {
        _powerPlanState = powerPlanState;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_powerPlanState is {} powerPlanState)
            {
                WindowsPowerPlan.Restore(powerPlanState);
                ConsoleOutput.WriteInfo("Benchmark restored", $"Power plan: {WindowsPowerPlan.FormatScheme(powerPlanState.PreviousPowerScheme)}; Processor AC: min: {powerPlanState.PreviousProcessorMinAc}%, max: {powerPlanState.PreviousProcessorMaxAc}%");
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput.WriteError("Failed to restore benchmark environment: " + ex.Message);
        }
    }

    public static RunEnvironment Apply(EnvironmentSettings settings, (string Label, string Value)[] extraRows, (string Label, string Value) cpuRow)
    {
        (string, string) osRow = ("OS", RuntimeInformation.OSDescription);
        (string, string) cpuCountRow = ("CPU", System.Environment.ProcessorCount == 1 ? "1 logical processor" : System.Environment.ProcessorCount + " logical processors");

        if (!WindowsPowerPlan.IsSupported)
        {
            ConsoleOutput.WriteBenchmarkSetup([osRow, cpuCountRow, ("Power plan", "skipped (not Windows)"), cpuRow, ..extraRows]);
            return new RunEnvironment(null);
        }

        WindowsPowerPlanState state = WindowsPowerPlan.Apply(settings.PowerPlan);
        ConsoleOutput.WriteBenchmarkSetup([osRow, cpuCountRow, ("Power plan", state.PowerPlanDisplay), ("Processor AC", state.ProcessorAcDisplay), cpuRow, ..extraRows]);
        return new RunEnvironment(state);
    }
}