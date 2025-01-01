using System.Reflection;
using Genbox.FastData.InternalShared;
using Microsoft.CodeAnalysis;

namespace Genbox.FastData.Tests.Code;

internal static class TestHelper
{
    private static readonly HashSet<string> _ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CS8019"
    };

    public static void TestResource<T>(string testName) where T : IIncrementalGenerator, new()
    {
        string inputSource = ReadResource(testName);

        string actual = GetGeneratedOutput<T>(inputSource).ReplaceLineEndings("\n");
        string expected = ReadResource(Path.ChangeExtension(testName, "output"));

        Assert.Equal(expected, actual);
    }

    public static string ReadResource(string name)
    {
        Assembly assembly = typeof(TestHelper).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(name);

        if (stream == null)
            throw new InvalidOperationException("Unable to find the resource " + name);

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string GetGeneratedOutput<T>(string source) where T : IIncrementalGenerator, new()
    {
        const string headers = """
                               using Genbox.FastData;
                               using Genbox.FastData.Enums;

                               """;

        //Add a few headers by default
        source = headers + source;

        string result = CodeGenerator.RunSourceGenerator<T>(source, false, out Diagnostic[] compilerDiag, out Diagnostic[] codeGenDiag);
        Assert.DoesNotContain(compilerDiag, x => !_ignore.Contains(x.Id));
        Assert.Empty(codeGenDiag);
        Assert.NotEmpty(result);

        return result;
    }
}