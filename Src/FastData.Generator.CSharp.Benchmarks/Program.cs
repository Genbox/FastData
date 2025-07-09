using System.Globalization;
using System.Text;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Generator.CSharp.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        string rootDir = Path.Combine(Path.GetTempPath(), "FastData", "CSharp");
        TestHelper.CreateOrEmptyDirectory(rootDir);

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

        foreach (ITestData data in TestVectorHelper.GetBenchmarkData())
        {
            data.Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), out GeneratorSpec spec);

            TestHelper.TryWriteFile(Path.Combine(rootDir, spec.Identifier + ".cs"), spec.Source);

            //CSharp duplicate in the name due to https://github.com/bencherdev/bencher/issues/605
            sb.AppendLine(CultureInfo.InvariantCulture, $$"""
                                                              [Benchmark]
                                                              public void CSharp_{{spec.Identifier}}()
                                                              {
                                                          {{PrintQueries(data, spec.Identifier)}}
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

    private static string PrintQueries(ITestData data, string identifier)
    {
        CSharpLanguageDef langDef = new CSharpLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, langDef.Encoding);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"        {identifier}.Contains({data.GetValueLabel(map)});");
        }

        return sb.ToString();
    }
}