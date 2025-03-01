using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class HashSetChain(FastDataConfig config, GeneratorContext context) : HashSetCode.IHashSetBase
{
    private int[] _buckets;
    private Entry[] _entries;

    public void Create(Func<object, uint> hashFunc)
    {
        int len = config.Data.Length;

        int[] buckets = new int[len];
        Entry[] entries = new Entry[len];

        for (int i = 0; i < len; i++)
        {
            object value = config.Data[i];
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

    public string Generate(IHashSpec? spec) =>
        $$"""
              private{{GetModifier(config.ClassType)}} readonly int[] _buckets = { {{JoinValues(_buckets, RenderBucket)}} };

              private{{GetModifier(config.ClassType)}} readonly Entry[] _entries = {
          {{JoinValues(_entries, RenderEntry, ",\n")}}
              };

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  uint hashCode = {{(spec != null ? "Hash(value)" : GetHashFunction32(config.DataType, "value"))}};
                  uint index = {{GetModFunction("hashCode", (uint)_buckets.Length)}};
                  int i = _buckets[index] - 1;

                  while (i >= 0)
                  {
                      ref Entry entry = ref _entries[i];

                      if (entry.HashCode == hashCode && {{GetEqualFunction(config.DataType, "entry.Value", "value")}})
                          return true;

                      i = entry.Next;
                  }

                  return false;
              }

          {{(spec != null ? spec.GetSource() : "")}}

              [StructLayout(LayoutKind.Auto)]
              private struct Entry
              {
                  public uint HashCode;
                  public {{(config.Data.Length <= short.MaxValue ? "short" : "int")}} Next;
                  public {{config.DataType}} Value;

                  public Entry(uint hashCode, {{(config.Data.Length <= short.MaxValue ? "short" : "int")}} next, {{config.DataType}} value)
                  {
                      HashCode = hashCode;
                      Next = next;
                      Value = value;
                  }
              }
          """;

    private static void RenderBucket(StringBuilder sb, int obj) => sb.Append(obj);
    private static void RenderEntry(StringBuilder sb, Entry obj) => sb.Append("        new Entry(").Append(obj.HashCode).Append(", ").Append(obj.Next).Append(", ").Append(ToValueLabel(obj.Value)).Append(')');

    [StructLayout(LayoutKind.Auto)]
    private struct Entry
    {
        public uint HashCode;
        public int Next;
        public object Value;
    }
}