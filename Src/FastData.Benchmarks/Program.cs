using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Genbox.FastData.Extensions;
using Genbox.FastData.Generator.CSharp;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        IConfig config = ManualConfig.CreateMinimumViable()
                                     .AddJob(new Job(new RunMode
                                     {
                                         LaunchCount = 1,
                                         WarmupCount = 1,
                                         MinIterationCount = 10,
                                         MaxIterationCount = 30
                                     }, Job.InProcess))
                                     .AddAnalyser(EnvironmentAnalyser.Default,
                                         MinIterationTimeAnalyser.Default,
                                         RuntimeErrorAnalyser.Default,
                                         BaselineCustomAnalyzer.Default,
                                         HideColumnsAnalyser.Default)
                                     .AddValidator(BaselineValidator.FailOnError,
                                         SetupCleanupValidator.FailOnError,
                                         JitOptimizationsValidator.FailOnError,
                                         RunModeValidator.FailOnError,
                                         GenericBenchmarksValidator.DontFailOnError,
                                         DeferredExecutionValidator.FailOnError,
                                         ParamsAllValuesValidator.FailOnError,
                                         ParamsValidator.FailOnError)
                                     .WithOption(ConfigOptions.DisableLogFile, true);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }

    public static void GenerateFiles()
    {
        // This code is used to generate the files in the Generated folder.

        int[] sizes = [1, 5, 10, 50, 100, 500, 1000];
        Type[] structures = [typeof(ArrayStructure<,>), typeof(ConditionalStructure<,>), typeof(BinarySearchStructure<,>), typeof(BinarySearchStructure<,>), typeof(HashTableStructure<,>)];

        foreach (Type type in structures)
        {
            foreach (int size in sizes)
            {
                DoStructure(type, size);
            }
        }
    }

    private static void DoStructure(Type type, int size)
    {
        Random rng = new Random(42);
        string[] data = Enumerable.Range(0, size).Select(_ => TestHelper.GenerateRandomString(rng, rng.Next(5, 10))).ToArray();

        TestVector<string> vector = new TestVector<string>(type, data, []);
        GeneratorSpec spec = TestHelper.Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), vector);
        File.WriteAllText("Gen-" + type.GetCleanName() + "-" + size + ".cs", spec.Source);
    }
}