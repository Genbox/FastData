using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal sealed class SingleValueCode(FastDataSpec Spec) : ICode
{
    public bool IsAppropriate(DataProperties dataProps) => Spec.Data.Length == 1;

    public bool TryPrepare() => true;

    //We don't support early exits in this generator.
    // - Strings: Length is checked in the equals function
    // - Integers: Only need an equals function (x == y)
    // - Others: They fall back to a simple equals as well

    public string Generate(IEnumerable<IEarlyExit> earlyExits)
        => $$"""
                 {{GetMethodAttributes()}}
                 public{{GetModifier(Spec.ClassType)}} bool Contains({{Spec.DataTypeName}} value)
                 {
                     return {{GetEqualFunction("value", ToValueLabel(Spec.Data[0]))}};
                 }
             """;
}