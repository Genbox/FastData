using System.Diagnostics;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class MinimalPerfectHashCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    private (object, uint)[] _data;
    private uint _seed;

    public bool TryCreate()
    {
        long timestamp = Stopwatch.GetTimestamp();

        //Find the proper seeds
        uint[] seed = MPHHelper.Generate(Spec.Data, static (x, y) => HashHelper.HashObjectSeed(x, y, true), 1, uint.MaxValue, Spec.Data.Length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 60;
        }).ToArray(); //We call .ToArray() as FirstOrDefault() would return 0 (in the default case), which is a valid seed.

        (object, uint)[] data = new (object, uint)[Spec.Data.Length];

        for (int i = 0; i < Spec.Data.Length; i++)
        {
            object value = Spec.Data[i];

            uint hash = HashHelper.HashObjectSeed(value, seed[0], true);
            uint index = (uint)(hash % Spec.Data.Length);
            data[index] = (value, hash);
        }

        _seed = seed[0];
        _data = data;
        return true;
    }

    public string Generate()
    {
        return $$"""
                     private{{GetModifier(Spec.ClassType)}} Entry[] _entries = new Entry[] {
                 {{JoinValues(_data, Render, ",\n")}}
                     };

                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", Context.GetEarlyExits())}}

                         uint hash = {{GetSeededHashFunction32(Spec.KnownDataType, "value", _seed, true)}};
                         uint index = {{GetModFunction("hash", (uint)_data.Length)}};
                         ref Entry entry = ref _entries[index];

                         return hash == entry.HashCode && {{GetEqualFunction("value", "entry.Value")}};
                     }

                     [StructLayout(LayoutKind.Auto)]
                     private struct Entry
                     {
                         public Entry({{Spec.DataTypeName}} value, uint hashCode)
                         {
                             Value = value;
                             HashCode = hashCode;
                         }

                         public {{Spec.DataTypeName}} Value;
                         public uint HashCode;
                     }
                 """;

        static void Render(StringBuilder sb, (object, uint) obj)
        {
            sb.Append("        new Entry(").Append(ToValueLabel(obj.Item1)).Append(", ").Append(obj.Item2).Append("u)");
        }
    }
}