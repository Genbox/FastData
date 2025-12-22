using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class BitSetCode<TKey, TValue>(BitSetContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}ulong[] _bitset = new ulong[] {
                    {{FormatColumns(ctx.BitSet, ToValueLabel)}}
                        };

                    """);

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            ulong offset = (ulong)(key - MinKey);
                            int word = (int)(offset >> 6);
                            return (_bitset[word] & (1UL << (int)(offset & 63))) != 0;
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}} value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                ulong offset = (ulong)(key - MinKey);
                                int word = (int)(offset >> 6);
                                if ((_bitset[word] & (1UL << (int)(offset & 63))) == 0)
                                {
                                    value = default;
                                    return false;
                                }

                                value = _values[(int)offset];
                                return true;
                            }
                        """);
        }

        return sb.ToString();
    }
}
