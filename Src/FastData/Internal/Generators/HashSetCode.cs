using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetCode(FastDataSpec Spec) : ICode
{
    private int[] _buckets;
    private Entry[] _entries;

    public bool IsAppropriate(DataProperties dataProps) => true;

    public bool TryPrepare()
    {
        int len = Spec.Data.Length;

        int[] buckets = new int[len];
        Entry[] entries = new Entry[len];

        for (int i = 0; i < len; i++)
        {
            object value = Spec.Data[i];
            uint hashCode = HashHelper.HashObject(value);
            ref int bucket = ref buckets[hashCode % len];

            ref Entry entry = ref entries[i];
            entry.HashCode = hashCode;
            entry.Next = bucket - 1; // Value in _buckets is 1-based
            entry.Value = value;
            bucket = i + 1;
        }

        _buckets = buckets;
        _entries = entries;
        return true;
    }

    public string Generate(IEnumerable<IEarlyExit> ee)
    {
        return $$"""
                     private{{GetModifier(Spec.ClassType)}} readonly int[] _buckets = { {{JoinValues(_buckets, RenderBucket)}} };

                     private{{GetModifier(Spec.ClassType)}} readonly Entry[] _entries = {
                 {{JoinValues(_entries, RenderEntry, ",\n")}}
                     };

                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", ee)}}

                         uint hashCode = {{GetHashFunction32(Spec.KnownDataType, "value")}};
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

    [StructLayout(LayoutKind.Auto)]
    private struct Entry
    {
        public uint HashCode;
        public int Next;
        public object Value;
    }
}