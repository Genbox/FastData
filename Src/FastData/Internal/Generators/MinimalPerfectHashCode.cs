using System.Diagnostics;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class MinimalPerfectHashCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        if (length >= 20)
            throw new InvalidOperationException("Not able to generate a MPF for more than 20 items");

        long timestamp = Stopwatch.GetTimestamp();

        //Find the proper seeds
        uint seed = MinimalPerfectHash.Generate(spec.Data, HashHelper.Hash, 1, ulong.MaxValue, length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 60;
        }).First();

        (string, uint)[] data = new (string, uint)[length];

        for (int i = 0; i < length; i++)
        {
            string value = spec.Data[i];

            uint hash = (uint)HashHelper.Hash(value, seed);
            uint index = hash % length;
            data[index] = (value, hash);
        }

        sb.Append($$"""
                        private{{staticStr}} Entry[] _entries = new[] {
                    {{GenerateList(data)}}
                        };

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            uint hash = {{GetHashFunction("value", seed)}};
                            uint index = {{GetModFunction("hash", length)}};
                            ref Entry entry = ref _entries[index];

                            return hash == entry.HashCode && {{GetEqualFunction("value", "entry.Value")}};
                        }

                        [StructLayout(LayoutKind.Auto)]
                        private struct Entry
                        {
                            public Entry(string value, uint hashCode)
                            {
                                Value = value;
                                HashCode = hashCode;
                            }

                            public string Value;
                            public uint HashCode;
                        }
                    """);
    }

    private static string GenerateList((string, uint)[] data)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            (string value, uint hash) = data[i];
            sb.Append("        new Entry(").Append('"').Append(value).Append("\", ").Append(hash).Append("u)");

            if (i != data.Length - 1)
                sb.AppendLine(",");
        }

        return sb.ToString();
    }
}