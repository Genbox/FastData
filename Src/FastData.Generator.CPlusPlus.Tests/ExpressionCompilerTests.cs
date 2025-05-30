using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CPlusPlus.Internal;
using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class ExpressionCompilerTests
{
    [Theory]
    [ClassData(typeof(ExpressionHashDataClass))]
    internal async Task GenerateExpression(HashType type, IStringHash hash)
    {
        CPlusPlusExpressionCompiler compiler = new CPlusPlusExpressionCompiler(new TypeHelper(new TypeMap(new CPlusPlusLanguageDef().TypeDefinitions)));

        string code = compiler.GetCode(hash.GetExpression());
        Assert.NotEmpty(code);

        await Verify(code)
              .UseFileName($"{hash.GetType().Name}-{type}")
              .UseDirectory("Expressions")
              .DisableDiff();
    }
}