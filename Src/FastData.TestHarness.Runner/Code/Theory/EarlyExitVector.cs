using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed record EarlyExitVector(IEarlyExit EarlyExit, object Match, object NoMatch, string? AdditionalId = null)
{
    public string SnapshotId => $"{nameof(EarlyExitVectors)}_{EarlyExit.GetType().Name}" + (AdditionalId != null ? $"_{AdditionalId}" : string.Empty);
    public string ProgramId => SanitizeId(SnapshotId);

    public override string ToString() => ProgramId;

    private static string SanitizeId(string value) => value.Replace('`', '_');
}