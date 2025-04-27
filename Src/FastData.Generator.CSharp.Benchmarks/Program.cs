using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Helpers;
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

                      [JsonExporterAttribute.Brief]
                      [ShortRunJob]
                      [SuppressMessage("Performance", "CA1822:Mark members as static")]
                      public class Program
                      {
                      """);

        foreach ((StructureType type, object[] data) in TestVectorHelper.GetBenchmarkVectors())
        {
            if (!TestVectorHelper.TryGenerate(id => new CSharpCodeGenerator(new CSharpGeneratorConfig(id)), type, data, out GeneratorSpec spec))
                throw new InvalidOperationException("Unable to build " + type);

            TestHelper.TryWriteFile(Path.Combine(rootDir, spec.Identifier + ".cs"), spec.Source);

            sb.AppendLine(CultureInfo.InvariantCulture,
                $"    [Benchmark] public bool BM_{spec.Identifier}() => {spec.Identifier}.Contains({ToValueLabel(data[0])});");
        }

        sb.AppendLine("""

                          internal static void Main() => BenchmarkRunner.Run<Program>();
                      }
                      """);

        TestHelper.TryWriteFile(Path.Combine(rootDir, "Program.cs"), sb.ToString());
        BenchmarkHelper.RunBenchmark("dotnet", "run -c release", rootDir, @"--adapter c_sharp_dot_net --file BenchmarkDotNet.Artifacts\results\CSharp.Program-report-brief.json");
    }
}