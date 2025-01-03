using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class BinarySearchCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        sb.Append($$"""
                        private{{staticStr}} {{spec.DataTypeName}}[] _entries = new {{spec.DataTypeName}}[] {
                    {{GenerateList(spec.Data)}}
                        };

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            int lo = 0;
                            int hi = {{(spec.Data.Length - 1).ToString(NumberFormatInfo.InvariantInfo)}};
                            while (lo <= hi)
                            {
                                int i = lo + ((hi - lo) >> 1);
                                int order = {{GetCompareFunction("_entries[i]", "value")}};

                                if (order == 0)
                                    return true;
                                if (order < 0)
                                    lo = i + 1;
                                else
                                    hi = i - 1;
                            }

                            return ((~lo) >= 0);
                        }
                    """);
    }

    private static string GenerateList(object[] data)
    {
        Array.Sort(data, StringComparer.Ordinal);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            sb.Append("        ").Append(ToValueLabel(data[i]));

            if (i != data.Length - 1)
                sb.AppendLine(", ");
        }

        return sb.ToString();
    }
}