using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal abstract class CSharpOutputWriter<T> : OutputWriter<T>
{
    internal string GetCompareFunction(string var1, string var2)
    {
        if (GeneratorConfig.DataType == DataType.String)
            return $"StringComparer.{GeneratorConfig.StringComparison}.Compare({var1}, {var2})";

        return $"{var1}.CompareTo({var2})";
    }

    internal string GetEqualFunction(string var1, string var2)
    {
        if (GeneratorConfig.DataType == DataType.String)
            return $"StringComparer.{GeneratorConfig.StringComparison}.Equals({var1}, {var2})";

        return $"{var1}.Equals({var2})";
    }
}