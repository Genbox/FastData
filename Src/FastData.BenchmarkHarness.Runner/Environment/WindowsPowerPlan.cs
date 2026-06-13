using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal static partial class WindowsPowerPlan
{
    private const byte AcLineOffline = 0;
    private const uint ErrorMoreData = 234;
    private static readonly Guid ProcessorSettingsSubgroup = new Guid("54533251-82be-4824-96c1-47b60b740d00");
    private static readonly Guid ProcessorThrottleMin = new Guid("893dee8e-2bef-41e0-89c6-b55d0929964c");
    private static readonly Guid ProcessorThrottleMax = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");

    public static bool IsSupported => OperatingSystem.IsWindows();

    public static WindowsPowerPlanState Apply(Guid powerPlan)
    {
        Guid? previousPowerScheme = null;
        uint? previousProcessorMinAc = null;
        uint? previousProcessorMaxAc = null;

        try
        {
            EnsureAcPower();
            previousPowerScheme = GetActivePowerScheme();

            // Read AC values from the currently active plan so we can restore them later.
            previousProcessorMinAc = ReadAcPowerValue(previousPowerScheme.Value, ProcessorThrottleMin);
            previousProcessorMaxAc = ReadAcPowerValue(previousPowerScheme.Value, ProcessorThrottleMax);

            WriteAcPowerValue(powerPlan, ProcessorThrottleMin, 100);
            WriteAcPowerValue(powerPlan, ProcessorThrottleMax, 100);
            SetActivePowerScheme(powerPlan);

            string powerPlanDisplay = previousPowerScheme.Value == powerPlan ? FormatScheme(powerPlan) : FormatScheme(previousPowerScheme.Value) + " -> " + FormatScheme(powerPlan);
            string processorAcDisplay = FormatAcValue("min", previousProcessorMinAc.Value) + ", " + FormatAcValue("max", previousProcessorMaxAc.Value);
            return new WindowsPowerPlanState(previousPowerScheme.Value, previousProcessorMinAc.Value, previousProcessorMaxAc.Value, powerPlanDisplay, processorAcDisplay);
        }
        catch
        {
            TryRestore(previousPowerScheme, previousProcessorMinAc, previousProcessorMaxAc);
            throw;
        }
    }

    public static void Restore(WindowsPowerPlanState state)
    {
        WriteAcPowerValue(state.PreviousPowerScheme, ProcessorThrottleMin, state.PreviousProcessorMinAc);
        WriteAcPowerValue(state.PreviousPowerScheme, ProcessorThrottleMax, state.PreviousProcessorMaxAc);
        SetActivePowerScheme(state.PreviousPowerScheme);
    }

    public static string FormatScheme(Guid scheme)
    {
        string? name = TryReadPowerSchemeName(scheme);
        return name ?? scheme.ToString("D");
    }

    private static void EnsureAcPower()
    {
        if (!GetSystemPowerStatus(out PowerStatus status))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetSystemPowerStatus failed.");

        if (status.AcLineStatus == AcLineOffline)
            throw new InvalidOperationException("Benchmark requires AC power, but Windows reports the system is running on battery.");
    }

    private static void TryRestore(Guid? previousPowerScheme, uint? previousProcessorMinAc, uint? previousProcessorMaxAc)
    {
        try
        {
            if (previousPowerScheme is null)
                return;

            if (previousProcessorMinAc is {} processorMinAc)
                WriteAcPowerValue(previousPowerScheme.Value, ProcessorThrottleMin, processorMinAc);

            if (previousProcessorMaxAc is {} processorMaxAc)
                WriteAcPowerValue(previousPowerScheme.Value, ProcessorThrottleMax, processorMaxAc);

            SetActivePowerScheme(previousPowerScheme.Value);
        }
        catch
        {
            // Preserve the original setup failure.
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

    private static string? TryReadPowerSchemeName(Guid scheme)
    {
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

    private static void ThrowIfPowerError(uint result, string apiName)
    {
        if (result == 0)
            return;

        throw new Win32Exception(checked((int)result), apiName + " failed.");
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