using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                            return {{GetEqualFunction("key", ToValueLabel(ctx.Item))}};
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add("classes", CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            private static readonly {{ValueTypeName}} _storedValue = {{ToValueLabel(ctx.Values[0])}};

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}} value)
                            {
                                if ({{GetEqualFunction("key", ToValueLabel(ctx.Item))}})
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