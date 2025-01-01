using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class SwitchHashCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;
        uint length = (uint)spec.Data.Length;

        (uint, string)[] hashCodes = new (uint, string)[length];

        for (int i = 0; i < spec.Data.Length; i++)
        {
            string value = spec.Data[i];
            uint hash = (uint)HashHelper.Hash(value);

            hashCodes[i] = (hash, value);
        }

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                            switch ({{GetHashFunction("value", 0)}})
                            {
                    {{GenerateSwitch(hashCodes)}}
                            }
                            return false;
                        }
                    """);
    }

    private static string GenerateSwitch((uint, string)[] data)
    {
        StringBuilder sb = new StringBuilder();

        foreach ((uint, string) pair in data)
        {
            sb.Append($"""

                                   case {pair.Item1.ToString(NumberFormatInfo.InvariantInfo)}:
                                        return {GetEqualFunction("value", $"\"{pair.Item2}\"")};
                       """);
        }

        return sb.ToString();
    }
}