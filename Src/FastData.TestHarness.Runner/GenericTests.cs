namespace Genbox.FastData.TestHarness.Runner;

public class GenericTests
{
    [Fact]
    public Task VerifyChecksTest() => VerifyChecks.Run();
}