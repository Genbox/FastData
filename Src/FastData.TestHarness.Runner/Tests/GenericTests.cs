namespace Genbox.FastData.TestHarness.Runner.Tests;

public class GenericTests
{
    [Fact]
    public Task VerifyChecksTest() => VerifyChecks.Run();
}