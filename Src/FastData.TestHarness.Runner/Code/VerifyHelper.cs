namespace Genbox.FastData.TestHarness.Runner.Code;

internal static class VerifyHelper
{
    internal static async Task VerifyFeatureAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/Features/" + harnessName)
              .DisableDiff()
              .ConfigureAwait(false);

    internal static async Task VerifyVectorAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/Vectors/" + harnessName)
              .DisableDiff()
              .ConfigureAwait(false);

    internal static async Task VerifyEarlyExitAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/EarlyExits/" + harnessName)
              .DisableDiff()
              .ConfigureAwait(false);

    internal static async Task VerifyEndToEndAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/EndToEnd/" + harnessName)
              .DisableDiff()
              .ConfigureAwait(false);

    internal static async Task VerifyStringHashAsync(string harnessName, string snapshotId, string source) =>
        await Verify(source)
              .UseFileName(snapshotId)
              .UseDirectory("../Verify/StringHash/" + harnessName)
              .DisableDiff()
              .ConfigureAwait(false);
}