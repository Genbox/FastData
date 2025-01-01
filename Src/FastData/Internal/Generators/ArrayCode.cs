using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class ArrayCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        sb.Append($$"""
                        private{{staticStr}} string[] _entries = new[] {
                    {{GenerateList(spec.Data)}}
                        };

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            for (int i = 0; i < {{length.ToString(NumberFormatInfo.InvariantInfo)}}; i++)
                            {
                                if ({{GetEqualFunction("value", "_entries[i]")}})
                                   return true;
                            }
                            return false;
                        }
                    """);
    }

    private static string GenerateList(string[] data)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            sb.Append("        \"").Append(data[i]).Append('"');

            if (i != data.Length - 1)
                sb.AppendLine(", ");
        }

        return sb.ToString();
    }
}