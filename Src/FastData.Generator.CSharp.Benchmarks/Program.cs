using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.Generator.CSharp.Internal.Helpers.CodeHelper;

namespace Genbox.FastData.Generator.CSharp.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CSharp");
        Directory.CreateDirectory(rootDir);

        TestHelper.TryWriteFile(Path.Combine(rootDir, "CSharp.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net9.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
              </ItemGroup>
            </Project>
            """);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("""
                      using System.Diagnostics.CodeAnalysis;
                      using BenchmarkDotNet.Running;
                      using BenchmarkDotNet.Attributes;

                      namespace CSharp;

                      [JsonExporterAttribute.BriefCompressed]
                      [ShortRunJob]
                      [SuppressMessage("Performance", "CA1822:Mark members as static")]
                      public class Program
                      {
                      """);

        foreach (ITestData data in TestVectorHelper.GetBenchmarkVectors())
        {
            if (!TestVectorHelper.TryGenerate(id => new CSharpCodeGenerator(new CSharpCodeGeneratorConfig(id)), data, out GeneratorSpec spec))
                throw new InvalidOperationException("Unable to build " + data.StructureType);

            TestHelper.TryWriteFile(Path.Combine(rootDir, spec.Identifier + ".cs"), spec.Source);

            //CSharp duplicate in the name due to https://github.com/bencherdev/bencher/issues/605
            sb.AppendLine(CultureInfo.InvariantCulture, $$"""
                                                              [Benchmark]
                                                              public void CSharp_{{spec.Identifier}}()
                                                              {
                                                          {{PrintQueries(data.Items, spec.Identifier)}}
                                                              }
                                                          """);
        }

        sb.AppendLine("""

                          internal static void Main() => BenchmarkRunner.Run<Program>();
                      }
                      """);

        TestHelper.TryWriteFile(Path.Combine(rootDir, "Program.cs"), sb.ToString());
        BenchmarkHelper.RunBenchmark("dotnet", "run -c release", rootDir, @"--adapter c_sharp_dot_net --file BenchmarkDotNet.Artifacts\results\CSharp.Program-report-brief-compressed.json --testbed CSharp");
    }

    private static string PrintQueries(object[] data, string identifier)
    {
        Random rng = new Random(42);
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"        {identifier}.Contains({ToValueLabel(data[rng.Next(0, data.Length)])});");

        return sb.ToString();
    }
}