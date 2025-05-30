using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Rust.Internal;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Generator.Rust.Tests;

public class ExpressionCompilerTests
{
    [Theory]
    [ClassData(typeof(ExpressionHashDataClass))]
    internal async Task GenerateExpression(HashType type, IStringHash hash)
    {
        RustExpressionCompiler compiler = new RustExpressionCompiler(new TypeHelper(new TypeMap(new RustLanguageDef().TypeDefinitions)));

        string code = compiler.GetCode(hash.GetExpression());
        Assert.NotEmpty(code);

        await Verify(code)
              .UseFileName($"{hash.GetType().Name}-{type}")
              .UseDirectory("Expressions")
              .DisableDiff();
    }
}