using System.Diagnostics;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CPlusPlus.Internal.Helpers;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.CPlusPlus.Tests;

public class VectorTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void Test(StructureType type, object[] data, string root)
    {
        Assert.True(TestHelper.TryGenerate(id => new CPlusPlusCodeGenerator(new CPlusPlusGeneratorConfig(id)), type, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        string file = Path.Combine(root, $"{spec.Identifier}.cpp");

        File.WriteAllText(file, $$"""
                                  #include <string>
                                  #include <iostream>

                                  {{spec.Source}}

                                  int main(int argc, char* argv[])
                                  {
                                      return {{spec.Identifier}}::contains({{CodeHelper.ToValueLabel(data[0])}});
                                  }
                                  """);

        //Compile it
        ProcessStartInfo info = new ProcessStartInfo();
        info.UseShellExecute = false;
        info.CreateNoWindow = true;

        info.FileName = "cmd.exe";
        info.Arguments = $"""
                          /c ""C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" && cd "{root}" && cl /EHsc /O1 /MT /DNDEBUG /wd4838 /permissive- /std:c++17 "{file}""
                          """;

        Process? compile = Process.Start(info);
        Assert.NotNull(compile);
        compile.WaitForExit();

        Assert.True(compile.ExitCode == 0, "Failed to compile. Exit code: " + compile.ExitCode);

        //Run it
        Process app = Process.Start(Path.ChangeExtension(file, ".exe"));
        app.WaitForExit();

        Assert.Equal(1, app.ExitCode);
    }

    public static TheoryData<StructureType, object[], string> GetTestData()
    {
        //This is placed here to make sure it is only run once
        string root = Path.Combine(Path.GetTempPath(), "FastData", "CPlusPlus");

        //Create or empty the directory
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);
        else
            TestHelper.EmptyDirectory(root);

        TheoryData<StructureType, object[], string> res = new TheoryData<StructureType, object[], string>();

        //Produce source code for test vectors
        foreach ((StructureType type, object[] data) in TestHelper.GetTestVectors())
            res.Add(type, data, root);

        return res;
    }
}