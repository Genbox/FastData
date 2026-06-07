using System.ComponentModel;
using System.Runtime.InteropServices;
using Genbox.FastData.BenchmarkHarness.Runner.Configuration;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal sealed partial class BenchmarkEnvironment : IDisposable
{
    private const byte AcLineOffline = 0;
    private const byte AcLineOnline = 1;
    private const uint ErrorMoreData = 234;
    private static readonly Guid ProcessorSettingsSubgroup = new Guid("54533251-82be-4824-96c1-47b60b740d00");
    private static readonly Guid ProcessorThrottleMin = new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c");
    private static readonly Guid ProcessorThrottleMax = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");
    private readonly Guid _benchmarkPowerScheme;
    private readonly Guid? _previousPowerScheme;
    private readonly uint? _previousProcessorMaxAc;
    private readonly uint? _previousProcessorMinAc;

    private bool _disposed;

    private BenchmarkEnvironment(Guid benchmarkPowerScheme, Guid? previousPowerScheme, uint? previousProcessorMinAc, uint? previousProcessorMaxAc)
    {
        _benchmarkPowerScheme = benchmarkPowerScheme;
        _previousPowerScheme = previousPowerScheme;
        _previousProcessorMinAc = previousProcessorMinAc;
        _previousProcessorMaxAc = previousProcessorMaxAc;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Exception? restoreException = null;

        try
        {
            if (OperatingSystem.IsWindows() && _previousPowerScheme is {} previousPowerScheme && _previousProcessorMinAc is {} previousProcessorMinAc && _previousProcessorMaxAc is {} previousProcessorMaxAc)
            {
                WriteAcPowerValue(_benchmarkPowerScheme, ProcessorThrottleMin, previousProcessorMinAc);
                WriteAcPowerValue(_benchmarkPowerScheme, ProcessorThrottleMax, previousProcessorMaxAc);
                SetActivePowerScheme(previousPowerScheme);
                BenchmarkConsole.WriteInfo("Benchmark restored", FormatRestoreMessage(previousPowerScheme, previousProcessorMinAc, previousProcessorMaxAc));
            }
        }
        catch (Exception ex)
        {
            restoreException = ex;
        }

        if (restoreException != null)
            throw new InvalidOperationException("Failed to restore benchmark environment.", restoreException);
    }

    public static BenchmarkEnvironment Apply(BenchmarkEnvironmentSettings settings, (string Label, string Value)[] cpuRows)
    {
        Guid? previousPowerScheme = null;
        uint? previousProcessorMinAc = null;
        uint? previousProcessorMaxAc = null;
        List<(string Label, string Value)> rows = GetHostRows();

        try
        {
            if (!OperatingSystem.IsWindows())
            {
                rows.Add(("Power plan", "skipped (not Windows)"));
                rows.AddRange(cpuRows);
                BenchmarkConsole.WriteBenchmarkSetup(rows.ToArray());
                return new BenchmarkEnvironment(settings.PowerPlan, null, null, null);
            }

            PowerStatus powerStatus = GetPowerStatus();
            if (powerStatus.AcLineStatus == AcLineOffline)
                throw new InvalidOperationException("Benchmark requires AC power, but Windows reports the system is running on battery.");

            previousPowerScheme = GetActivePowerScheme();
            previousProcessorMinAc = ReadAcPowerValue(settings.PowerPlan, ProcessorThrottleMin);
            previousProcessorMaxAc = ReadAcPowerValue(settings.PowerPlan, ProcessorThrottleMax);

            if (previousPowerScheme != settings.PowerPlan)
                SetActivePowerScheme(settings.PowerPlan);

            rows.Add(("Power plan", FormatTransition(previousPowerScheme.Value, settings.PowerPlan)));

            WriteAcPowerValue(settings.PowerPlan, ProcessorThrottleMin, 100);
            WriteAcPowerValue(settings.PowerPlan, ProcessorThrottleMax, 100);
            SetActivePowerScheme(settings.PowerPlan);

            rows.Add(("Processor AC", FormatProcessorAcTransition(previousProcessorMinAc.Value, previousProcessorMaxAc.Value)));

            rows.AddRange(cpuRows);
            BenchmarkConsole.WriteBenchmarkSetup(rows.ToArray());

            return new BenchmarkEnvironment(settings.PowerPlan, previousPowerScheme, previousProcessorMinAc, previousProcessorMaxAc);
        }
        catch
        {
            TryRestorePowerState(settings.PowerPlan, previousPowerScheme, previousProcessorMinAc, previousProcessorMaxAc);
            throw;
        }
    }

    private static Guid GetActivePowerScheme()
    {
        uint result = PowerGetActiveScheme(IntPtr.Zero, out IntPtr schemePointer);
        ThrowIfPowerError(result, nameof(PowerGetActiveScheme));

        try
        {
            return Marshal.PtrToStructure<Guid>(schemePointer);
        }
        finally
        {
            if (schemePointer != IntPtr.Zero)
                LocalFree(schemePointer);
        }
    }

    private static string FormatRestoreMessage(Guid powerScheme, uint processorMinAc, uint processorMaxAc) =>
        $"Power plan: {FormatPowerScheme(powerScheme)}; Processor AC: min: {processorMinAc}%, max: {processorMaxAc}%";

    private static string FormatTransition(Guid previous, Guid current) =>
        previous == current ? FormatPowerScheme(current) : FormatPowerScheme(previous) + " -> " + FormatPowerScheme(current);

    private static string FormatProcessorAcTransition(uint processorMinAc, uint processorMaxAc) =>
        FormatAcValue("min", processorMinAc) + ", " + FormatAcValue("max", processorMaxAc);

    private static string FormatAcValue(string label, uint value) => value == 100 ? label + ": 100%" : $"{label}: {value}% -> 100%";

    private static void SetActivePowerScheme(Guid scheme)
    {
        Guid localScheme = scheme;
        uint result = PowerSetActiveScheme(IntPtr.Zero, ref localScheme);
        ThrowIfPowerError(result, nameof(PowerSetActiveScheme));
    }

    private static uint ReadAcPowerValue(Guid scheme, Guid setting)
    {
        Guid localScheme = scheme;
        Guid localSubgroup = ProcessorSettingsSubgroup;
        Guid localSetting = setting;
        uint result = PowerReadACValueIndex(IntPtr.Zero, ref localScheme, ref localSubgroup, ref localSetting, out uint value);
        ThrowIfPowerError(result, nameof(PowerReadACValueIndex));
        return value;
    }

    private static void WriteAcPowerValue(Guid scheme, Guid setting, uint value)
    {
        Guid localScheme = scheme;
        Guid localSubgroup = ProcessorSettingsSubgroup;
        Guid localSetting = setting;
        uint result = PowerWriteACValueIndex(IntPtr.Zero, ref localScheme, ref localSubgroup, ref localSetting, value);
        ThrowIfPowerError(result, nameof(PowerWriteACValueIndex));
    }

    private static List<(string Label, string Value)> GetHostRows() =>
    [
        ("OS", RuntimeInformation.OSDescription),
        ("CPU", System.Environment.ProcessorCount == 1 ? "1 logical processor" : System.Environment.ProcessorCount + " logical processors")
    ];

    private static string FormatPowerScheme(Guid scheme)
    {
        string? name = TryReadPowerSchemeName(scheme);
        return name ?? scheme.ToString("D");
    }

    private static string? TryReadPowerSchemeName(Guid scheme)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        Guid localScheme = scheme;
        uint bufferSize = 0;
        uint result = PowerReadFriendlyName(IntPtr.Zero, ref localScheme, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bufferSize);

        if ((result != 0 && result != ErrorMoreData) || bufferSize == 0)
            return null;

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)bufferSize));

        try
        {
            result = PowerReadFriendlyName(IntPtr.Zero, ref localScheme, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize);
            if (result != 0)
                return null;

            string? name = Marshal.PtrToStringUni(buffer);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static PowerStatus GetPowerStatus()
    {
        if (!GetSystemPowerStatus(out PowerStatus status))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetSystemPowerStatus failed.");

        return status;
    }

    private static string FormatAcLineStatus(byte status) => status switch
    {
        AcLineOffline => "offline",
        AcLineOnline => "online",
        _ => "unknown"
    };

    private static void ThrowIfPowerError(uint result, string apiName)
    {
        if (result == 0)
            return;

        throw new Win32Exception(checked((int)result), apiName + " failed.");
    }

    private static void TryRestorePowerState(Guid benchmarkPowerScheme, Guid? previousPowerScheme, uint? previousProcessorMinAc, uint? previousProcessorMaxAc)
    {
        try
        {
            if (!OperatingSystem.IsWindows() || previousPowerScheme is not {} powerScheme)
                return;

            if (previousProcessorMinAc is {} processorMinAc)
                WriteAcPowerValue(benchmarkPowerScheme, ProcessorThrottleMin, processorMinAc);

            if (previousProcessorMaxAc is {} processorMaxAc)
                WriteAcPowerValue(benchmarkPowerScheme, ProcessorThrottleMax, processorMaxAc);

            SetActivePowerScheme(powerScheme);
        }
        catch
        {
            // Preserve the original setup failure.
        }
    }

    [LibraryImport("PowrProf.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [LibraryImport("PowrProf.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

    [LibraryImport("PowrProf.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerReadACValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subgroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint acValueIndex);

    [LibraryImport("PowrProf.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerWriteACValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subgroupOfPowerSettingsGuid, ref Guid powerSettingGuid, uint acValueIndex);

    [LibraryImport("PowrProf.dll", EntryPoint = "PowerReadFriendlyName")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subgroupOfPowerSettingsGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemPowerStatus(out PowerStatus systemPowerStatus);

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial IntPtr LocalFree(IntPtr memory);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PowerStatus
    {
        public readonly byte AcLineStatus;
        public readonly byte BatteryFlag;
        public readonly byte BatteryLifePercent;
        public readonly byte SystemStatusFlag;
        public readonly uint BatteryLifeTime;
        public readonly uint BatteryFullLifeTime;
    }
}