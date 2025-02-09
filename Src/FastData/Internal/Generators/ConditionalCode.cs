using System.Text;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class ConditionalCode(FastDataSpec Spec, GeneratorContext Context) : ICode
{
    public bool TryCreate() => true;

    public string Generate()
    {
        return $$"""
                     {{GetMethodAttributes()}}
                     public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                     {
                 {{GetEarlyExits("value", Context.GetEarlyExits())}}

                         if ({{JoinValues(Spec.Data, Render, " || ")}})
                             return true;

                         return false;
                     }
                 """;

        static void Render(StringBuilder sb, object obj) => sb.Append(GetEqualFunction("value", ToValueLabel(obj)));
    }
}