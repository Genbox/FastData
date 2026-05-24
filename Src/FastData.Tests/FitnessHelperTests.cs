using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Tests;

public class FitnessHelperTests
{
    [Fact]
    public void UnconstrainedSegmentUsesEffectiveLengthFitness()
    {
        string[] data = ["abcd", "wxyz"];
        Expression expression = Expression.Constant(1UL);

        double singleUnitFitness = FitnessHelper.CalculateFitness(
            KeyAnalyzer.GetStringProperties(data, false, GeneratorEncoding.Utf16CodeUnits),
            new ArraySegment(0, 1, Alignment.Left),
            expression);

        double fullSegmentFitness = FitnessHelper.CalculateFitness(
            KeyAnalyzer.GetStringProperties(data, false, GeneratorEncoding.Utf16CodeUnits),
            new ArraySegment(0, -1, Alignment.Left),
            expression);

        Assert.True(singleUnitFitness > fullSegmentFitness);
    }
}