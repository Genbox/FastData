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
         using System;
         using System.Diagnostics;
         using System.Globalization;

         {data.Generate(Bootstrap.Generator)}

         {Bootstrap.Wrap($$"""
                                  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
                                  static T BlackBox<T>(T value)
                                  {
                                      GC.KeepAlive(value);
                                      return value;
                                  }

                                  var keys = new[] { {{FormatList(data.GetQuerySet(Bootstrap.Map), s => s)}} };
                                  double[] results = new double[{{data.SampleCount}}];

                                  double MeasureSample()
                                  {
                                      int keyIndex = 0;
                                      int foundCount = 0;

                                      long startTicks = Stopwatch.GetTimestamp();

                                      for (int i = 0; i < {{data.WorkIterations}}; i++)
                                      {
                                  {{FormatList(Range(0, data.QueryCount).ToArray(), _ =>
                                      """
                                              {
                                                  var key = BlackBox(keys[keyIndex]);
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
                                      int expectedFoundCount = {{data.WorkIterations * data.QueryCount}};
                                      if (foundCount != expectedFoundCount)
                                      {
                                          Console.Error.WriteLine($"Expected {expectedFoundCount} matches, got {foundCount}.");
                                          Environment.Exit(2);
                                      }

                                      return elapsed;
                                  }

                                  for (int i = 0; i < {{data.WarmupCount}}; i++)
                                      GC.KeepAlive(MeasureSample());

                                  GC.Collect();
                                  GC.WaitForPendingFinalizers();
                                  GC.Collect();

                                  double sum = 0;
                                  for (int i = 0; i < results.Length; i++)
                                  {
                                      results[i] = MeasureSample();
                                      sum += results[i];
                                  }

                                  Array.Sort(results);
                                  double avg = sum / results.Length;
                                  Console.WriteLine(results[0].ToString("R", CultureInfo.InvariantCulture) + " " +
                                                    results[results.Length / 2].ToString("R", CultureInfo.InvariantCulture) + " " +
                                                    results[^1].ToString("R", CultureInfo.InvariantCulture) + " " +
                                                    avg.ToString("R", CultureInfo.InvariantCulture));
                                  return 0;
                           """)}
         """;
}