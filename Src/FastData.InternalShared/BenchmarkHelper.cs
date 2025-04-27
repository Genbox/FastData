namespace Genbox.FastData.InternalShared;

public static class BenchmarkHelper
{
    public static void RunBenchmark(string program, string args, string workingDir, string bencherArgs)
    {
        int res;

        //We check if bencher is available.
        if (TestHelper.TryRunProcess("bencher", "--version"))
        {
            //The BENCHER_API_TOKEN must be set
            if (Environment.GetEnvironmentVariable("BENCHER_API_TOKEN") == null)
                throw new InvalidOperationException("BENCHER_API_TOKEN must be set");

            res = TestHelper.RunProcess("bencher", $"run {bencherArgs} \"{program} {args}\"", workingDir);
        }
        else
            res = TestHelper.RunProcess(program, args, workingDir);

        if (res != 0)
            throw new InvalidOperationException($"Failed to run benchmarks. Return code: {res}");
    }
}