using System.Security.Cryptography;
using System.Text;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Harness;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.TestHarness.Runner.Code;

internal static class TestHarnessHelper
{
    private const int CompileHashBytes = 8;
    private const int SuccessExitCode = 1;
    private const string FeatureDirectory = "../Verify/Features/";
    private const string VectorDirectory = "../Verify/Vectors/";

    internal static async Task VerifyFeatureAsync(TestBase harness, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory(FeatureDirectory + harness.Name)
              .DisableDiff();

    internal static async Task VerifyVectorAsync(TestBase harness, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory(VectorDirectory + harness.Name)
              .DisableDiff();

    internal static int RunContains<T>(TestBase harness, GeneratorSpec spec, T[] present, T[] notPresent, string fileId)
    {
        string program = harness.RenderContains(spec, present, notPresent);
        string compileId = GetCompileId(harness, fileId, program);
        return harness.Run(compileId, program);
    }

    internal static int RunTryLookup<TKey, TValue>(TestBase harness, GeneratorSpec spec, TestVector<TKey, TValue> vector, string fileId) where TValue : notnull
    {
        string program = harness.RenderTryLookup(spec, vector);
        string compileId = GetCompileId(harness, fileId, program);
        return harness.Run(compileId, program);
    }

    internal static void AssertSuccessExitCode(int exitCode) => Assert.Equal(SuccessExitCode, exitCode);

    private static string GetCompileId(TestBase harness, string fileId, string source)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        string hashHex = Convert.ToHexString(hash.AsSpan(0, CompileHashBytes));
        return $"{harness.Name}_{fileId}_{hashHex}";
    }
}