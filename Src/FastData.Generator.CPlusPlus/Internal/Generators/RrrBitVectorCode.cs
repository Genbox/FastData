using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class RrrBitVectorCode<TKey>(RrrBitVectorContext ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        _ = ctx;

        return $$"""
                public:
                    {{MethodAttribute}}
                    {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} {{InputKeyName}}){{PostMethodModifier}} {
                {{GetMethodHeader(MethodType.Contains)}}
                        return false;
                    }
                """;
    }
}
