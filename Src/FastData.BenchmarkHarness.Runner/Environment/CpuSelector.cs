using System.Runtime.InteropServices;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal static partial class CpuSelector
{
    private const uint ProcessorInformationRelationshipProcessorCore = 0;

    private const int UInt64BitsPerInteger = 64;
    private const uint ErrorInsufficientBuffer = 122;

    public static CpuSelection? TryGetSelection()
    {
        if (!TryGetSelections(1, out CpuSelection[] selections, out _))
            return null;

        return selections[0];
    }

    public static bool TryGetSelections(int count, out CpuSelection[] selections, out int availableCoreCount)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be a positive integer.");

        selections = [];
        availableCoreCount = 0;

        if (!OperatingSystem.IsWindows())
            return TryGetLogicalProcessorSelections(count, out selections, out availableCoreCount);

        if (!TryGetLogicalProcessorTopology(out CoreTopology[] cores))
            return TryGetLogicalProcessorSelections(count, out selections, out availableCoreCount);

        if (cores.Length == 0)
            return TryGetLogicalProcessorSelections(count, out selections, out availableCoreCount);

        int targetCoreIndex = Math.Max(1, cores.Length / 2);
        CpuCandidate[] candidates = GetSelectableCandidates(cores, targetCoreIndex);
        availableCoreCount = candidates.Length;

        if (availableCoreCount == 0)
            return TryGetLogicalProcessorSelections(count, out selections, out availableCoreCount);

        selections = candidates.Take(count).Select(x => new CpuSelection(x.LogicalProcessor)).ToArray();
        return true;
    }

    private static CpuCandidate[] GetSelectableCandidates(CoreTopology[] cores, int targetCoreIndex)
    {
        List<CpuCandidate> candidates = new List<CpuCandidate>();

        for (int coreIndex = 0; coreIndex < cores.Length; coreIndex++)
        {
            CoreTopology core = cores[coreIndex];
            int logicalProcessor = core.LogicalProcessors.FirstOrDefault(x => x != 0, -1);
            if (logicalProcessor >= 0)
                candidates.Add(new CpuCandidate(logicalProcessor, coreIndex, core.LogicalProcessors.Length));
        }

        return candidates.OrderBy(x => x.Siblings)
                         .ThenBy(x => x.CoreIndex == 0)
                         .ThenBy(x => Math.Abs(x.CoreIndex - targetCoreIndex))
                         .ThenBy(x => x.LogicalProcessor)
                         .ToArray();
    }

    private static bool TryGetLogicalProcessorSelections(int count, out CpuSelection[] selections, out int availableCoreCount)
    {
        int logicalProcessorCount = System.Environment.ProcessorCount;
        int firstProcessor = logicalProcessorCount > 1 ? 1 : 0;
        availableCoreCount = logicalProcessorCount - firstProcessor;

        if (availableCoreCount <= 0)
        {
            selections = [];
            return false;
        }

        selections = Enumerable.Range(firstProcessor, Math.Min(count, availableCoreCount)).Select(x => new CpuSelection(x)).ToArray();
        return true;
    }

    private static bool TryGetLogicalProcessorTopology(out CoreTopology[] cores)
    {
        int logicalProcessorCount = System.Environment.ProcessorCount;
        cores = Array.Empty<CoreTopology>();

        if (logicalProcessorCount <= 1)
            return false;

        if (logicalProcessorCount > UInt64BitsPerInteger)
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

                for (int processorIndex = 0; processorIndex < logicalProcessorCount; processorIndex++)
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

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        public readonly UIntPtr ProcessorMask;
        public readonly uint Relationship;
        public readonly uint ProcessorCoreFlags;
        public readonly uint Reserved1;
        public readonly uint Reserved2;
        public readonly uint Reserved3;
    }
}