namespace Genbox.FastData.TestHarness.Runner.Code;

internal static class VerifyHelper
{
    internal static async Task VerifyFeatureAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/Features/" + harnessName)
              .DisableDiff();

    internal static async Task VerifyVectorAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/Vectors/" + harnessName)
              .DisableDiff();
}