using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Termination;

namespace Genbox.FastData.Tests.Genetics;

public class TimeBasedTerminationTests
{
    [Fact]
    public void DoesNotExpireBeforeDuration()
    {
        TimeBasedTermination termination = new TimeBasedTermination(TimeSpan.FromMinutes(1));

        Thread.Sleep(5);

        Assert.False(termination.Process(0, 0));
    }

    [Fact]
    public void ExpiresWhenDurationIsZero()
    {
        TimeBasedTermination termination = new TimeBasedTermination(TimeSpan.Zero);

        Assert.True(termination.Process(0, 0));
    }
}