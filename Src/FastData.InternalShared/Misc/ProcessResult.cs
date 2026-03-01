namespace Genbox.FastData.InternalShared.Misc;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);