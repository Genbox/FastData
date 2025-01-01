using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using static Genbox.FastData.Internal.CodeSnip;

namespace Genbox.FastData.Internal.Generators;

internal static class UniqueKeyLengthSwitchCode
{
    public static void Generate(StringBuilder sb, FastDataSpec spec, IEnumerable<IEarlyExitSpec> earlyExitSpecs)
    {
        string? staticStr = spec.ClassType == ClassType.Static ? " static" : null;

        //Sanity check on inputs
        HashSet<int> uniqLen = new HashSet<int>();

        foreach (string value in spec.Data)
        {
            if (!uniqLen.Add(value.Length))
                throw new InvalidOperationException("Not able to generate a unique length index as the data does not have unique lengths");
        }

        sb.Append($$"""
                        {{GetMethodAttributes()}}
                        public{{staticStr}} bool Contains(string value)
                        {
                    {{GetEarlyExits("value", earlyExitSpecs)}}

                            switch (value.Length)
                            {
                    {{GenerateSwitch(spec.Data)}}
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
        {
            sb.AppendLine($"""
                                       case {value.Length}:
                                           return {GetEqualFunction("value", $"\"{value}\"")};
                           """);
        }

        return sb.ToString();
    }
}