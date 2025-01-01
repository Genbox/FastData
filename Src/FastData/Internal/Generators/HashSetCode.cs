using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class HashSetCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        int[] buckets = new int[length];
        Entry[] entries = new Entry[length];

        for (int i = 0; i < length; i++)
        {
            string value = spec.Data[i];

            uint hashCode = (uint)HashHelper.Hash(value);
            ref int bucket = ref buckets[hashCode % length];

            ref Entry entry = ref entries[i];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1; // Value in _buckets is 1-based
            entry.Value = value;
            bucket = i + 1;
        }

        bool small = length <= short.MaxValue;

        sb.Append($$"""
                    {{GenerateBuckets(buckets, staticStr)}}
                    {{GenerateEntries(entries, staticStr)}}

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            uint hashCode = {{GetHashFunction("value", 0)}};
                            uint index = {{GetModFunction("hashCode", (uint)buckets.Length)}};
                            int i = _buckets[index] - 1;

                            while (i >= 0)
                            {
                                ref Entry entry = ref _entries[i];

                                if (entry.HashCode == hashCode && {{GetEqualFunction("entry.Value", "value")}})
                                    return true;

                                i = entry.Next;
                            }

                            return false;
                        }

                        [StructLayout(LayoutKind.Auto)]
                        private struct Entry
                        {
                            public uint HashCode;
                            public {{(small ? "short" : "int")}} Next;
                            public string Value;

                            public Entry(uint hashCode, {{(small ? "short" : "int")}} next, string value)
                            {
                                HashCode = hashCode;
                                Next = next;
                                Value = value;
                            }
                        }
                    """);
    }

    private static string GenerateBuckets(int[] data, string? staticStr)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"    private{staticStr} readonly int[] _buckets = {{ ");

        for (int i = 0; i < data.Length; i++)
        {
            sb.Append(data[i]);

            if (i != data.Length - 1)
                sb.Append(", ");
        }

        sb.AppendLine(" };");
        return sb.ToString();
    }

    private static string GenerateEntries(Entry[] data, string? staticStr)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"    private{staticStr} readonly Entry[] _entries = {{ ");

        for (int i = 0; i < data.Length; i++)
        {
            ref Entry entry = ref data[i];
            sb.Append("        new Entry(").Append(entry.HashCode).Append(", ").Append(entry.Next).Append(", \"").Append(entry.Value).Append("\")");

            if (i != data.Length - 1)
                sb.Append(',');

            sb.AppendLine();
        }

        sb.Append("    };");
        return sb.ToString();
    }

    [StructLayout(LayoutKind.Auto)]
    private struct Entry
    {
        public uint HashCode;
        public int Next;
        public string Value;
    }
}