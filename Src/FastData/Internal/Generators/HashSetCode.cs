using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.BruteForce;
using Genbox.FastData.Internal.Analysis.Genetic;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    private int[] _buckets;
    private Entry[] _entries;
    private IHashSpec? _hashSpec;

    public bool TryCreate()
    {
        if (Spec.KnownDataType == KnownDataType.String)
        {
            DataProperties props = Context.GetDataProperties();

            BruteForceAnalyzer analyzer = new BruteForceAnalyzer(Spec.Data, props.StringProps.Value, new BruteForceSettings(), RunSimulation);
            Candidate<BruteForceHashSpec> candidate = analyzer.Run();

            Func<string, uint> hashFunc = candidate.Spec.GetFunction();
            _hashSpec = candidate.Spec;
            Create(x => hashFunc((string)x));
        }
        else
            Create(HashHelper.HashObject);

        return true;
    }

    internal static void RunSimulation<T>(object[] data, CommonSettings settings, ref Candidate<T> candidate) where T : struct, IHashSpec
    {
        // Generate a hash function from the spec
        Func<string, uint> hashFunc = candidate.Spec.GetFunction();

        int capacity = (int)(data.Length * settings.CapacityFactor);
        string first = (string)data[0];

        long ticks = Stopwatch.GetTimestamp();
        //Set power plan to high performance
        //Pin process to 1 core
        //Set process priority to above normal
        for (int i = 0; i < 1000; i++)
            hashFunc(first);
        ticks = Stopwatch.GetTimestamp() - ticks;

        (int occupied, double minVariance, double maxVariance) = Emulate(data, capacity, hashFunc);

        double normOccu = (occupied / (double)capacity) * settings.FillWeight;
        double normTime = (1.0 / (1.0 + ((double)ticks / 1000))) * settings.TimeWeight;

        candidate.Fitness = (normOccu + normTime) / 2;
        candidate.Metadata = [("Time/norm", ticks + "/" + normTime.ToString("N2")), ("Occupied/norm", occupied + "/" + normOccu.ToString("N2")), ("MinVariance", minVariance), ("MaxVariance", maxVariance)];
    }

    public string Generate()
    {
        return $$"""
                     private{{GetModifier(Spec.ClassType)}} readonly int[] _buckets = { {{JoinValues(_buckets, RenderBucket)}} };

                     private{{GetModifier(Spec.ClassType)}} readonly Entry[] _entries = {
                 {{JoinValues(_entries, RenderEntry, ",\n")}}
                     };

                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", Context.GetEarlyExits())}}

                         uint hashCode = {{(_hashSpec != null ? "Hash(value)" : GetHashFunction32(Spec.KnownDataType, "value"))}};
                         uint index = {{GetModFunction("hashCode", (uint)_buckets.Length)}};
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

                 {{(_hashSpec != null ? _hashSpec.GetSource() : "")}}

                     [StructLayout(LayoutKind.Auto)]
                     private struct Entry
                     {
                         public uint HashCode;
                         public {{(Spec.Data.Length <= short.MaxValue ? "short" : "int")}} Next;
                         public {{Spec.DataTypeName}} Value;

                         public Entry(uint hashCode, {{(Spec.Data.Length <= short.MaxValue ? "short" : "int")}} next, {{Spec.DataTypeName}} value)
                         {
                             HashCode = hashCode;
                             Next = next;
                             Value = value;
                         }
                     }
                 """;

        static void RenderBucket(StringBuilder sb, int obj) => sb.Append(obj);

        static void RenderEntry(StringBuilder sb, Entry obj) => sb.Append("        new Entry(").Append(obj.HashCode).Append(", ").Append(obj.Next).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');
    }

    private static (int cccupied, double minVariance, double maxVariance) Emulate(object[] data, int capacity, Func<string, uint> hashFunc)
    {
        int[] buckets = new int[capacity];

        for (int i = 0; i < capacity; i++)
            buckets[hashFunc((string)data[i]) % buckets.Length]++;

        int occupied = 0;
        double minVariance = double.MaxValue;
        double maxVariance = double.MinValue;

        for (int i = 0; i < buckets.Length; i++)
        {
            int bucket = buckets[i];

            if (bucket > 0)
                occupied++;

            minVariance = Math.Min(minVariance, bucket);
            maxVariance = Math.Max(maxVariance, bucket);
        }

        return (occupied, minVariance, maxVariance);
    }

    private void Create(Func<object, uint> hashFunc)
    {
        int len = Spec.Data.Length;

        int[] buckets = new int[len];
        Entry[] entries = new Entry[len];

        for (int i = 0; i < len; i++)
        {
            object value = Spec.Data[i];
            uint hashCode = hashFunc(value);
            ref int bucket = ref buckets[hashCode % len];

            ref Entry entry = ref entries[i];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1; // Value in _buckets is 1-based
            entry.Value = value;
            bucket = i + 1;
        }

        _buckets = buckets;
        _entries = entries;
    }

    [StructLayout(LayoutKind.Auto)]
    private struct Entry
    {
        public uint HashCode;
        public int Next;
        public object Value;
    }
}