using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class EytzingerSearchCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        Array.Sort(spec.Data);

        object[] output = new object[length];
        int index = 0;
        EytzingerOrder(spec.Data, output, ref index);

        sb.Append($$"""
                        private{{staticStr}} {{spec.DataTypeName}}[] _entries = new {{spec.DataTypeName}}[] {
                    {{GenerateList(output)}}
                        };

                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            int i = 0;
                            while (i < _entries.Length)
                            {
                                int comparison = {{GetCompareFunction("_entries[i]", "value")}};

                                if (comparison == 0)
                                    return true;

                                if (comparison < 0)
                                    i = 2 * i + 2;
                                else
                                    i = 2 * i + 1;
                            }

                            return false;
                        }
                    """);
    }

    private static string GenerateList(object[] data)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            sb.Append("        ").Append(ToValueLabel(data[i]));

            if (i != data.Length - 1)
                sb.AppendLine(", ");
        }

        return sb.ToString();
    }

    private static void EytzingerOrder(object[] input, object[] output, ref int arrIdx, int eytIdx = 0)
    {
        if (eytIdx < input.Length)
        {
            EytzingerOrder(input, output, ref arrIdx, (2 * eytIdx) + 1);
            output[eytIdx] = input[arrIdx++];
            EytzingerOrder(input, output, ref arrIdx, (2 * eytIdx) + 2);
        }
    }
}