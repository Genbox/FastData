using System.Runtime.InteropServices;

namespace Genbox.FastData.BenchmarkHarness.Runner.Environment;

internal static partial class CpuSelector
{
    private const uint ProcessorInformationRelationshipProcessorCore = 0;

    private const int UInt64BitsPerInteger = 64;
    private const uint ErrorInsufficientBuffer = 122;

    public static CpuSelection? TryGetSelection()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        if (!TryGetLogicalProcessorTopology(out CoreTopology[] cores))
            return null;

        if (cores.Length == 0)
            return null;

        int logicalProcessorCount = System.Environment.ProcessorCount;
        int targetCoreIndex = Math.Max(1, cores.Length / 2);

        CpuCandidate selectedCandidate = new CpuCandidate(0, 0, 0);
        bool hasSelection = false;
        for (int coreIndex = 0; coreIndex < cores.Length; coreIndex++)
        {
            CoreTopology core = cores[coreIndex];

            foreach (int logicalProcessor in core.LogicalProcessors)
            {
                if (logicalProcessor == 0)
                    continue;

                CpuCandidate candidate = new CpuCandidate(logicalProcessor, coreIndex, core.LogicalProcessors.Length);

                if (!hasSelection)
                {
                    selectedCandidate = candidate;
                    hasSelection = true;
                    continue;
                }

                if (candidate.Siblings < selectedCandidate.Siblings)
                {
                    selectedCandidate = candidate;
                    continue;
                }

                if (candidate.Siblings > selectedCandidate.Siblings)
                    continue;

                if (candidate.CoreIndex != 0 && selectedCandidate.CoreIndex == 0)
                {
                    selectedCandidate = candidate;
                    continue;
                }

                if (candidate.CoreIndex == 0 && selectedCandidate.CoreIndex != 0)
                    continue;

                int candidateDistance = Math.Abs(candidate.CoreIndex - targetCoreIndex);
                int selectedDistance = Math.Abs(selectedCandidate.CoreIndex - targetCoreIndex);

                if (candidateDistance < selectedDistance)
                {
                    selectedCandidate = candidate;
                    continue;
                }

                if (candidateDistance > selectedDistance)
                    continue;

                if (candidate.LogicalProcessor < selectedCandidate.LogicalProcessor)
                    selectedCandidate = candidate;
            }
        }

        if (!hasSelection)
            return null;

        return new CpuSelection(selectedCandidate.LogicalProcessor, selectedCandidate.CoreIndex, selectedCandidate.Siblings, logicalProcessorCount, cores.Length);
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