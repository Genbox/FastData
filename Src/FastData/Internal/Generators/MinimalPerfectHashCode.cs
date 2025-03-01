using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Helpers;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class MinimalPerfectHashCode(FastDataConfig config, GeneratorContext context) : ICode
{
    private (object, uint)[] _data;
    private uint _seed;

    public bool TryCreate()
    {
        long timestamp = Stopwatch.GetTimestamp();

        //Find the proper seeds
        uint[] seed = MPHHelper.Generate(config.Data, static (x, y) => Mix(HashHelper.HashObjectSeed(x, y)), 1, uint.MaxValue, config.Data.Length, () =>
        {
            TimeSpan span = new TimeSpan(Stopwatch.GetTimestamp() - timestamp);
            return span.TotalSeconds > 60;
        }).ToArray(); //We call .ToArray() as FirstOrDefault() would return 0 (in the default case), which is a valid seed.

        (object, uint)[] data = new (object, uint)[config.Data.Length];

        for (int i = 0; i < config.Data.Length; i++)
        {
            object value = config.Data[i];

            uint hash = Mix(HashHelper.HashObjectSeed(value, seed[0]));
            uint index = (uint)(hash % config.Data.Length);
            data[index] = (value, hash);
        }

        _seed = seed[0];
        _data = data;
        return true;
    }

    public string Generate() =>
        $$"""
              private{{GetModifier(config.ClassType)}} Entry[] _entries = new Entry[] {
          {{JoinValues(_data, Render, ",\n")}}
              };

              {{GetMethodAttributes()}}
              public{{GetModifier(config.ClassType)}} bool Contains({{config.DataType}} value)
              {
          {{GetEarlyExits("value", context.GetEarlyExits())}}

                  uint hash = Mix({{GetSeededHashFunction32(config.DataType, "value", _seed)}});
                  uint index = {{GetModFunction("hash", (uint)_data.Length)}};
                  ref Entry entry = ref _entries[index];

                  return hash == entry.HashCode && {{GetEqualFunction(config.DataType, "value", "entry.Value")}};
              }

              [MethodImpl(MethodImplOptions.AggressiveInlining)]
              private static uint Mix(uint h)
              {
                  h ^= h >> 16;
                  h *= 0x85ebca6b;
                  h ^= h >> 13;
                  h *= 0xc2b2ae35;
                  h ^= h >> 16;
                  return h;
              }

              [StructLayout(LayoutKind.Auto)]
              private struct Entry
              {
                  public Entry({{config.DataType}} value, uint hashCode)
                  {
                      Value = value;
                      HashCode = hashCode;
                  }

                  public {{config.DataType}} Value;
                  public uint HashCode;
              }
          """;

    private static void Render(StringBuilder sb, (object, uint) obj) => sb.Append("        new Entry(").Append(ToValueLabel(obj.Item1)).Append(", ").Append(obj.Item2).Append("u)");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Mix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}