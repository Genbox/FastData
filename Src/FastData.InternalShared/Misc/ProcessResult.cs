namespace Genbox.FastData.InternalShared.Misc;

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);