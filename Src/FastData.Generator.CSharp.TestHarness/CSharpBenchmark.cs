using System.Globalization;
using System.Text;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBenchmark : BenchmarkBase<CSharpBootstrap>
{
    private CSharpBenchmark() : base(new CSharpBootstrap(HarnessType.Benchmark)) {}

    public static CSharpBenchmark Instance { get; } = new CSharpBenchmark();

    public override BenchmarkSuite CreateFiles(IEnumerable<ITestData> data)
    {
        List<BenchmarkFile> files =
        [
            new BenchmarkFile("CSharp.csproj", """
                                               <Project Sdk="Microsoft.NET.Sdk">
                                                 <PropertyGroup>
                                                   <OutputType>Exe</OutputType>
                                                   <TargetFramework>net10.0</TargetFramework>
                                                   <Nullable>enable</Nullable>
                                                 </PropertyGroup>

                                                 <ItemGroup>
                                                   <PackageReference Include="BenchmarkDotNet" Version="0.15.8" />
                                                 </ItemGroup>
                                               </Project>
                                               """)
        ];

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

        foreach (ITestData item in data)
        {
            item.Generate(Bootstrap.GeneratorFactory, out GeneratorSpec spec);
            files.Add(new BenchmarkFile(spec.Identifier + ".cs", spec.Source));

            sb.AppendLine(CultureInfo.InvariantCulture, $$"""
                                                              [Benchmark]
                                                              public void CSharp_{{spec.Identifier}}()
                                                              {
                                                          {{RenderBenchmarkQueries(item, spec.Identifier)}}
                                                              }
                                                          """);
        }

        sb.AppendLine("""

                          internal static void Main() => BenchmarkRunner.Run<Program>();
                      }
                      """);

        return new BenchmarkSuite("Program.cs", sb.ToString(), files);
    }

    public override void Run(BenchmarkSuite suite, bool useBencher, bool useShell)
    {
        BenchmarkHelper.RunBenchmark("dotnet", "run -c release", Bootstrap.RootDir, @"--adapter c_sharp_dot_net --file BenchmarkDotNet.Artifacts\results\CSharp.Program-report-brief-compressed.json --testbed CSharp", useBencher, useShell);
    }

    private string RenderBenchmarkQueries(ITestData data, string identifier)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 25; i++)
            sb.AppendLine(CultureInfo.InvariantCulture, $"        {identifier}.Contains({data.GetValueLabel(Bootstrap.Map)});");

        return sb.ToString();
    }
}