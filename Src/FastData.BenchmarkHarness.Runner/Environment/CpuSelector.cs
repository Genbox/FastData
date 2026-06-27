using System.Globalization;
using System.Runtime.InteropServices;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal static partial class CpuSelector
{
    private const uint ProcessorInformationRelationshipProcessorCore = 0;
    private const uint ErrorInsufficientBuffer = 122;

    public static bool TryGetCpuSet(out string? cpuSet)
    {
        cpuSet = null;

        if (!OperatingSystem.IsWindows())
            return TryGetLogicalProcessorCpuSet(out cpuSet);

        if (!TryGetLogicalProcessorTopology(out CoreTopology[] cores) || cores.Length == 0)
            return TryGetLogicalProcessorCpuSet(out cpuSet);

        int targetCoreIndex = Math.Max(1, cores.Length / 2);
        CpuCandidate? best = GetBestCandidate(cores, targetCoreIndex);

        if (best is null)
            return TryGetLogicalProcessorCpuSet(out cpuSet);

        cpuSet = FormatCpuIndex(best.Value.LogicalProcessor);
        return true;
    }

    private static CpuCandidate? GetBestCandidate(CoreTopology[] cores, int targetCoreIndex)
    {
        CpuCandidate? best = null;

        for (int coreIndex = 0; coreIndex < cores.Length; coreIndex++)
        {
            CoreTopology core = cores[coreIndex];
            int logicalProcessor = core.LogicalProcessors.FirstOrDefault(x => x != 0, -1);

            if (logicalProcessor < 0)
                continue;

            CpuCandidate candidate = new CpuCandidate(logicalProcessor, coreIndex, core.LogicalProcessors.Length);

            if (best is null || IsBetterCandidate(candidate, best.Value, targetCoreIndex))
                best = candidate;
        }

        return best;
    }

    private static bool IsBetterCandidate(CpuCandidate candidate, CpuCandidate current, int targetCoreIndex)
    {
        // Prefer fewer siblings (physical cores without hyperthreading).
        if (candidate.Siblings != current.Siblings)
            return candidate.Siblings < current.Siblings;

        // Avoid core 0 (often handles interrupts and OS scheduler work).
        bool candidateIsCore0 = candidate.CoreIndex == 0;
        bool currentIsCore0 = current.CoreIndex == 0;

        if (candidateIsCore0 != currentIsCore0)
            return !candidateIsCore0;

        // Prefer cores closest to the middle of the topology.
        int candidateDistance = Math.Abs(candidate.CoreIndex - targetCoreIndex);
        int currentDistance = Math.Abs(current.CoreIndex - targetCoreIndex);

        if (candidateDistance != currentDistance)
            return candidateDistance < currentDistance;

        // Tiebreaker: lowest logical processor index.
        return candidate.LogicalProcessor < current.LogicalProcessor;
    }

    private static bool TryGetLogicalProcessorCpuSet(out string? cpuSet)
    {
        int logicalProcessorCount = System.Environment.ProcessorCount;

        if (logicalProcessorCount <= 1)
        {
            cpuSet = null;
            return false;
        }

        // Skip processor 0 to avoid OS scheduler noise.
        cpuSet = FormatCpuIndex(1);
        return true;
    }

    private static string FormatCpuIndex(int logicalProcessor) => logicalProcessor.ToString(CultureInfo.InvariantCulture);

    private static bool TryGetLogicalProcessorTopology(out CoreTopology[] cores)
    {
        int logicalProcessorCount = System.Environment.ProcessorCount;
        cores = [];

        if (logicalProcessorCount <= 1)
            return false;

        uint bufferSize = 0;
        if (!GetLogicalProcessorInformation(IntPtr.Zero, ref bufferSize) && Marshal.GetLastWin32Error() != ErrorInsufficientBuffer)
            return false;

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)bufferSize));
        try
        {
            if (!GetLogicalProcessorInformation(buffer, ref bufferSize))
                return false;

            int entrySize = Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>();
            if (entrySize <= 0)
                return false;

            int entryCount = checked((int)(bufferSize / (uint)entrySize));
            List<CoreTopology> coreList = new List<CoreTopology>();

            // Cap iteration to 64 bits since ProcessorMask is a UIntPtr (64-bit max on x64).
            int maxProcessorIndex = Math.Min(logicalProcessorCount, 64);

            for (int i = 0; i < entryCount; i++)
            {
                IntPtr entryPointer = IntPtr.Add(buffer, i * entrySize);
                SYSTEM_LOGICAL_PROCESSOR_INFORMATION entry = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>(entryPointer);

                if (entry.Relationship != ProcessorInformationRelationshipProcessorCore)
                    continue;

                List<int> logicalProcessors = new List<int>();
                ulong processorMask = UIntPtr.Size == sizeof(long)
                    ? entry.ProcessorMask.ToUInt64()
                    : entry.ProcessorMask.ToUInt32();

                for (int processorIndex = 0; processorIndex < maxProcessorIndex; processorIndex++)
                {
                    if ((processorMask & (1UL << processorIndex)) == 0)
                        continue;

                    logicalProcessors.Add(processorIndex);
                }

                if (logicalProcessors.Count > 0)
                    coreList.Add(new CoreTopology(logicalProcessors.ToArray()));
            }

            if (coreList.Count == 0)
                return false;

            cores = coreList.ToArray();
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLogicalProcessorInformation(IntPtr buffer, ref uint returnedLength);

    private readonly record struct CoreTopology(int[] LogicalProcessors);

    private readonly record struct CpuCandidate(int LogicalProcessor, int CoreIndex, int Siblings);

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private readonly struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        [FieldOffset(0)]
        public readonly UIntPtr ProcessorMask;

        [FieldOffset(8)]
        public readonly uint Relationship;

        // Bytes 12-15 are padding on x64. The union starts at offset 16.
    }
}