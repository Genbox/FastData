using Genbox.FastData.Internal;
using Genbox.FastData.InternalShared;
using Microsoft.CodeAnalysis;

namespace Genbox.FastData.Tests;

public class FeatureTests
{
    [Theory, MemberData(nameof(GetInputs))]
    public void GenerateFeature(string inputFile)
    {
        string input = File.ReadAllText(inputFile);

        string code = CodeGenerator.RunSourceGenerator<FastDataGenerator>(input, false, out Diagnostic[] diag, out Diagnostic[] diag2);
        Assert.Empty(diag);
        Assert.Empty(diag2);

        File.WriteAllText($@"..\..\..\Generated\Features\{Path.GetFileNameWithoutExtension(inputFile)}.output", code);
    }

    public static TheoryData<string> GetInputs()
    {
        TheoryData<string> data = new TheoryData<string>();
        data.AddRange(Directory.GetFiles(@"..\..\..\Generated\Features\", "*.input"));
        return data;
    }
}