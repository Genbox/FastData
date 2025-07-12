using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashTablePerfectCode<TKey, TValue>(HashTablePerfectContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate() => ctx.StoreHashCode
        ? $$"""
                {{FieldModifier}}E[] _entries = {
            {{FormatColumns(ctx.Data, x => $"new E({ToValueLabel(x.Key)}, {x.Value.ToStringInvariant()})")}}
                };

                {{MethodAttribute}}
                {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                {
            {{EarlyExits}}

                    {{HashSizeType}} hash = Hash(key);
                    {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                    ref E entry = ref _entries[index];

                    return {{GetEqualFunction("hash", "entry.HashCode")}} && {{GetEqualFunction("key", "entry.Key")}};
                }

            {{HashSource}}

                [StructLayout(LayoutKind.Auto)]
                private struct E
                {
                    internal E({{KeyTypeName}} key, {{HashSizeType}} hashCode)
                    {
                        Key = key;
                        HashCode = hashCode;
                    }

                    internal {{KeyTypeName}} Key;
                    internal {{HashSizeType}} HashCode;
                }
            """
        : $$"""
                {{FieldModifier}}{{KeyTypeName}}[] _entries = {
            {{FormatColumns(ctx.Data, x => ToValueLabel(x.Key))}}
                };

                {{MethodAttribute}}
                {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                {
            {{EarlyExits}}

                    {{HashSizeType}} hash = Hash(key);
                    {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};

                    return {{GetEqualFunction("key", "_entries[index]")}};
                }

            {{HashSource}}
            """;
}