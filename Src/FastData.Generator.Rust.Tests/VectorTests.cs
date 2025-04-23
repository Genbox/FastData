using System.Diagnostics;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generator.Rust.Internal.Helpers;
using TestHelper = Genbox.FastData.Generator.Helpers.TestHelper;

namespace Genbox.FastData.Generator.Rust.Tests;

public class VectorTests
{
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void Test(StructureType type, object[] data, string root)
    {
        Assert.True(TestHelper.TryGenerate(id => new RustCodeGenerator(new RustGeneratorConfig(id)), type, data, out GeneratorSpec spec));
        Assert.NotEmpty(spec.Source);

        string file = Path.Combine(root, $"{spec.Identifier}.rs");

        File.WriteAllText(file, $$"""
                                  #![allow(non_camel_case_types)]
                                  {{spec.Source}}

                                  fn main() {
                                      let result = if {{spec.Identifier}}::contains({{CodeHelper.ToValueLabel(data[0])}}) { 1 } else { 0 };
                                      std::process::exit(result);
                                  }
                                  """);

        string output = Path.ChangeExtension(file, "exe");

        //Compile it
        ProcessStartInfo info = new ProcessStartInfo();
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.FileName = @"C:\Users\Genbox\.cargo\bin\rustc.exe";
        info.Arguments = $"{file} -o {output} -C debuginfo=0 -C link-args=/DEBUG:NONE";

        Process? compile = Process.Start(info);
        Assert.NotNull(compile);
        compile.WaitForExit();

        Assert.True(compile.ExitCode == 0, "Failed to compile. Exit code: " + compile.ExitCode);

        //Run it
        Process app = Process.Start(output);
        app.WaitForExit();

        Assert.Equal(1, app.ExitCode);
    }

    public static TheoryData<StructureType, object[], string> GetTestData()
    {
        //This is placed here to make sure it is only run once
        string root = Path.Combine(Path.GetTempPath(), "FastData", "Rust");

        //Create or empty the directory
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);
        else
            TestHelper.EmptyDirectory(root);

        TheoryData<StructureType, object[], string> res = new TheoryData<StructureType, object[], string>();

        foreach ((StructureType type, object[] data) in TestHelper.GetTestVectors())
            res.Add(type, data, root);

        return res;
    }
}