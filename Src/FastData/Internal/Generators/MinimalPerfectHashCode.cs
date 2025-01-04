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
        uint seed = MinimalPerfectHash.Generate(spec.Data, static (x, y) => HashHelper.HashObjectSeed(x, y, true), 1, uint.MaxValue, length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 60;
        }).First();

        (object, uint)[] data = new (object, uint)[length];

        for (int i = 0; i < length; i++)
        {
            object value = spec.Data[i];

            uint hash = HashHelper.HashObjectSeed(value, seed, true);
            uint index = hash % length;
            data[index] = (value, hash);
        }

        sb.Append($$"""
                        private{{staticStr}} Entry[] _entries = new Entry[] {
                    {{GenerateList(data)}}
                        };

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            uint hash = {{GetSeededHashFunction32(spec.KnownDataType, "value", seed, true)}};
                            uint index = {{GetModFunction("hash", length)}};
                            ref Entry entry = ref _entries[index];

                            return hash == entry.HashCode && {{GetEqualFunction("value", "entry.Value")}};
                        }

                        [StructLayout(LayoutKind.Auto)]
                        private struct Entry
                        {
                            public Entry({{spec.DataTypeName}} value, uint hashCode)
                            {
                                Value = value;
                                HashCode = hashCode;
                            }

                            public {{spec.DataTypeName}} Value;
                            public uint HashCode;
                        }
                    """);
    }

    private static string GenerateList((object, uint)[] data)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            (object value, uint hash) = data[i];
            sb.Append("        new Entry(").Append(ToValueLabel(value)).Append(", ").Append(hash).Append("u)");

            if (i != data.Length - 1)
                sb.AppendLine(",");
        }

        return sb.ToString();
    }
}