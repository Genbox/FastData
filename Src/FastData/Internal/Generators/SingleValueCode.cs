using System.Text;
using Genbox.FastData.Enums;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class SingleValueCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        if (length > 1)
            throw new InvalidOperationException("More than one value is not supported by SingleValue");

        //Note: The early-exit for length is redundant, as Equals also does a length check. That will change in the future.

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                            if (value.Length != {{spec.Data[0].Length}})
                                return false;

                            return {{GetEqualFunction("value", $"\"{spec.Data[0]}\"")}};
                        }
                    """);
    }
}