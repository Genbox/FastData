using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Internal;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class ExpressionCompilerTests
{
    [Theory]
    [ClassData(typeof(ExpressionHashTheoryData))]
    internal async Task GenerateExpression(HashType type, IStringHash hash)
    {
        CSharpExpressionCompiler compiler = new CSharpExpressionCompiler(new TypeHelper(new TypeMap(new CSharpLanguageDef().TypeDefinitions)));

        string code = compiler.GetCode(hash.GetExpression());
        Assert.NotEmpty(code);

        await Verify(code)
              .UseFileName($"{hash.GetType().Name}-{type}")
              .UseDirectory("Expressions")
              .DisableDiff();
    }
}