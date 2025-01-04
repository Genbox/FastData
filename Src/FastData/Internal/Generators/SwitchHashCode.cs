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

        (uint, object)[] hashCodes = new (uint, object)[length];

        for (int i = 0; i < spec.Data.Length; i++)
        {
            object value = spec.Data[i];
            uint hash = HashHelper.HashObject(value);

            hashCodes[i] = (hash, value);
        }

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains({{spec.DataTypeName}} value)
                        {
                            switch ({{GetHashFunction32(spec.KnownDataType, "value")}})
                            {
                    {{GenerateSwitch(hashCodes)}}
                            }
                            return false;
                        }
                    """);
    }

    private static string GenerateSwitch((uint, object)[] data)
    {
        StringBuilder sb = new StringBuilder();

        foreach ((uint, object) pair in data)
        {
            sb.Append($"""

                                   case {pair.Item1.ToString(NumberFormatInfo.InvariantInfo)}:
                                        return {GetEqualFunction("value", ToValueLabel(pair.Item2))};
                       """);
        }

        return sb.ToString();
    }
}