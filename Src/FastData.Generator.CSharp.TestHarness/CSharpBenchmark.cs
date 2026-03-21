using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using static System.Linq.Enumerable;
using static Genbox.FastData.Generator.Helpers.FormatHelper;

namespace Genbox.FastData.Generator.CSharp.TestHarness;

public sealed class CSharpBenchmark(DockerManager dockerManager) : BenchmarkBase<CSharpBootstrap>(new CSharpBootstrap(HarnessType.Benchmark), dockerManager)
{
    protected override string Render(ITestData data) =>
        $"""
         using System.Diagnostics;
         using System.Globalization;

         {data.Generate(Bootstrap.Generator)}

         {Bootstrap.Wrap($$"""
                                   var keys = new[] { {{FormatList(Range(0, data.QueryCount)
                                                                   .Select(_ => data.GetRandomKey(Bootstrap.Map))
                                                                   .ToArray(), s => s)}} };
                                   int keyIndex = 0;

                                   // Warmup
                                   for (int i = 0; i < {{data.WarmupIterations}}; i++)
                                   {
                                       var key = keys[keyIndex];
                                       keyIndex++;
                                       if (keyIndex == keys.Length)
                                           keyIndex = 0;

                                       FastData.Contains(key);
                                   }

                                   GC.Collect();
                                   GC.WaitForPendingFinalizers();
                                   GC.Collect();

                                   int foundCount = 0;

                                   long startTicks = Stopwatch.GetTimestamp();

                                   for (int i = 0; i < {{data.WorkIterations}}; i++)
                                   {
                           {{FormatList(Range(0, data.QueryCount).ToArray(), _ =>
                               """
                                       {
                                           var key = keys[keyIndex];
                                           keyIndex++;
                                           if (keyIndex == keys.Length)
                                               keyIndex = 0;

                                           foundCount += FastData.Contains(key) ? 1 : 0;
                                       }
                               """, "\n")}}
                                   }

                                   long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
                                   double ticksPerCall = (double)elapsedTicks / {{Bootstrap.Map.ToValueLabel(data.WorkIterations * data.QueryCount)}};
                                   double elapsed = ticksPerCall * 1_000_000_000d / Stopwatch.Frequency;

                                   GC.KeepAlive(foundCount);
                                   Console.WriteLine(elapsed.ToString("R", CultureInfo.InvariantCulture));
                                   return 0;
                           """)}
         """;
}