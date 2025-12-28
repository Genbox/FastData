using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());
            sb.Append($"    private static readonly {ValueTypeName} _storedValue = {ToValueLabel(values[0])};");
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} {{InputKeyName}})
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            return {{GetEqualFunction(LookupKeyName, ToValueLabel(ctx.Item))}};
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} {{InputKeyName}}, out {{ValueTypeName}} value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                if ({{GetEqualFunction(LookupKeyName, ToValueLabel(ctx.Item))}})
                                {
                                    value = _storedValue;
                                    return true;
                                }

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}