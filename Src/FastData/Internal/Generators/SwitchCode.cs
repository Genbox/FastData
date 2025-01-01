using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class SwitchCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            switch (value)
                            {
                    {{GenerateSwitch(spec.Data)}}
                                    return true;
                                default:
                                    return false;
                            }
                        }
                    """);
    }

    private static string GenerateSwitch(string[] values)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string value in values)
            sb.AppendLine($"            case \"{value}\":");

        return sb.ToString();
    }
}