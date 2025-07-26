using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}{{KeyTypeName}}[] _keys = new {{KeyTypeName}}[] {
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        };

                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetEarlyExits(MethodType.Contains)}}

                            for (int i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                            {
                                if ({{GetEqualFunction("key", "_keys[i]")}})
                                   return true;
                            }
                            return false;
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}}? value)
                            {
                                value = default;

                        {{GetEarlyExits(MethodType.TryLookup)}}

                                for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                                {
                                    if ({{GetEqualFunction("_keys[i]", "key")}})
                                    {
                                        value = _values[i];
                                        return true;
                                    }
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}