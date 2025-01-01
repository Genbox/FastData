using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class ConditionalCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            if ({{GenerateConditional("value", spec.Data)}})
                                return true;

                            return false;
                        }
                    """);
    }

    private static string GenerateConditional(string variable1, string[] values)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < values.Length; i++)
        {
            sb.Append(GetEqualFunction(variable1, $"\"{values[i]}\""));

            if (i != values.Length - 1)
                sb.Append(" || ");
        }

        return sb.ToString();
    }
}