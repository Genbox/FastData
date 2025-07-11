using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate() => ctx.StoreHashCode
        ? $$"""
                {{FieldModifier}}E[] _entries = {
            {{FormatColumns(ctx.Data, x => $"new E({ToValueLabel(x.Key)}, {x.Value.ToStringInvariant()})")}}
                };

                {{MethodAttribute}}
                {{MethodModifier}}bool Contains({{TypeName}} value)
                {
            {{EarlyExits}}

                    {{HashSizeType}} hash = Hash(value);
                    {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};
                    ref E entry = ref _entries[index];

                    return {{GetEqualFunction("hash", "entry.HashCode")}} && {{GetEqualFunction("value", "entry.Value")}};
                }

            {{HashSource}}

                [StructLayout(LayoutKind.Auto)]
                private struct E
                {
                    internal E({{TypeName}} value, {{HashSizeType}} hashCode)
                    {
                        Value = value;
                        HashCode = hashCode;
                    }

                    internal {{TypeName}} Value;
                    internal {{HashSizeType}} HashCode;
                }
            """
        : $$"""
                {{FieldModifier}}{{TypeName}}[] _entries = {
            {{FormatColumns(ctx.Data, x => ToValueLabel(x.Key))}}
                };

                {{MethodAttribute}}
                {{MethodModifier}}bool Contains({{TypeName}} value)
                {
            {{EarlyExits}}

                    {{HashSizeType}} hash = Hash(value);
                    {{ArraySizeType}} index = {{GetModFunction("hash", (ulong)ctx.Data.Length)}};

                    return {{GetEqualFunction("value", "_entries[index]")}};
                }

            {{HashSource}}
            """;
}