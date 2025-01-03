using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Enums;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class SingleValueCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec)
    {
        //We don't support early exits in this generator.
        // - Strings: Length is checked in the equals function
        // - Integers: Only need an equals function (x == y)
        // - Others: They fall back to a simple equals as well

        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        if (length > 1)
            throw new InvalidOperationException("More than one value is not supported by SingleValue");

        //Note: The early-exit for length is redundant, as Equals also does a length check. That will change in the future.

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                            return {{GetEqualFunction("value", ToValueLabel(spec.Data[0]))}};
                        }
                    """);
    }
}