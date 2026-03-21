using Genbox.FastData.Generator.Compat;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal sealed class RustEarlyExitDef(ExpressionCompiler compiler) : IEarlyExitDef
{
    public string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits, MethodType methodType, string keyName)
    {
        StringBuilder sb = new StringBuilder();

        // foreach (IEarlyExit earlyExit in earlyExits)
            // sb.AppendJoin(" && ", compiler.GetCode(earlyExit.GetExpression(keyName), 0));

        string eeStr = sb.ToString();
        return eeStr.Length == 0 ? string.Empty : RenderExit(methodType, sb.ToString());
    }

    private static string RenderExit(MethodType methodType, string condition) => methodType == MethodType.TryLookup
        ? $"if {condition} {{ return None; }}"
        : $"if {condition} {{ return false; }}";
}