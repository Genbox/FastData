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
    protected override string Render(ITestData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        BenchmarkQuerySet querySet = data.GetQuerySet(Bootstrap.Map);

        return $"""
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

                                        var keys = new[] { {{FormatList(querySet.Keys, s => s)}} };
                                        double[] results = new double[{{data.SampleCount}}];

                                        double MeasureSample(out long foundCount)
                                        {
                                            int keyIndex = 0;
                                            foundCount = 0;

                                            long startTicks = Stopwatch.GetTimestamp();

                                            for (int i = 0; i < {{data.WorkIterations}}; i++)
                                            {
                                        {{FormatList(Range(0, data.QueryCount).ToArray(), _ =>
                                            """
                                                    {
                                                        foundCount += FastData.Contains(BlackBox(keys[keyIndex])) ? 1 : 0;
                                                        keyIndex++;
                                                        if (keyIndex == keys.Length)
                                                            keyIndex = 0;
                                                    }
                                            """, "\n")}}
                                            }

                                            long elapsedTicks = Stopwatch.GetTimestamp() - startTicks;

                                            GC.KeepAlive(foundCount);

                                            return ((double)elapsedTicks / {{((long)data.WorkIterations * data.QueryCount).ToString(System.Globalization.CultureInfo.InvariantCulture)}}d) * 1_000_000_000d / Stopwatch.Frequency;
                                        }

                                        for (int i = 0; i < {{data.WarmupCount}}; i++)
                                        {
                                            GC.KeepAlive(MeasureSample(out long warmupFoundCount));
                                            GC.KeepAlive(warmupFoundCount);
                                        }

                                        GC.Collect();
                                        GC.WaitForPendingFinalizers();
                                        GC.Collect();

                                        double sum = 0;
                                        long totalFoundCount = 0;
                                        for (int i = 0; i < results.Length; i++)
                                        {
                                            results[i] = MeasureSample(out long sampleFoundCount);
                                            totalFoundCount += sampleFoundCount;
                                            sum += results[i];
                                        }

                                        Array.Sort(results);
                                        Console.WriteLine(results[0].ToString("R", CultureInfo.InvariantCulture) + " " +
                                                          results[results.Length / 2].ToString("R", CultureInfo.InvariantCulture) + " " +
                                                          results[^1].ToString("R", CultureInfo.InvariantCulture) + " " +
                                                          (sum / results.Length).ToString("R", CultureInfo.InvariantCulture) + " " +
                                                          totalFoundCount.ToString(CultureInfo.InvariantCulture));
                                        return 0;
                                  """)}
                """;
    }
}