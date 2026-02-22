using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestHarness;
using Xunit.Sdk;

namespace Genbox.FastData.TestHarness.Runner.Code;

internal static class TestHarnessRunnerHelper
{
    private const int SuccessExitCode = 1;
    private const string FeatureDirectory = "../Verify/Features/";
    private const string VectorDirectory = "../Verify/Vectors/";

    internal static async Task VerifyFeatureAsync(ITestHarness harness, string snapshotId, string source)
    {
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory(FeatureDirectory + harness.Name)
              .DisableDiff();
    }

    internal static async Task VerifyVectorAsync(ITestHarness harness, string snapshotId, string source)
    {
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory(VectorDirectory + harness.Name)
              .DisableDiff();
    }

    internal static int RunContainsProgram<T>(ITestHarness harness, GeneratorSpec spec, T[] present, T[] notPresent, string fileId)
    {
        ITestRenderer renderer = harness.CreateRenderer(spec);
        string program = harness.RenderContainsProgram(spec, renderer, present, notPresent);
        return harness.Run(fileId, program);
    }

    internal static int RunTryLookupProgram<TKey, TValue>(ITestHarness harness, GeneratorSpec spec, TestVector<TKey, TValue> vector, string fileId) where TValue : notnull
    {
        ITestRenderer renderer = harness.CreateRenderer(spec);
        string program = harness.RenderTryLookupProgram(spec, renderer, vector);
        return harness.Run(fileId, program);
    }

    internal static void AssertSuccessExitCode(int exitCode) => Assert.Equal(SuccessExitCode, exitCode);
}