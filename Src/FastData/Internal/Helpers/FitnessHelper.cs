using System.Linq.Expressions;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Helpers;

internal static class FitnessHelper
{
    internal static double CalculateFitness(StringProperties props, ArraySegment segment, Expression expression)
    {
        //The length of segment is a factor
        int minLen = (int)props.LengthData.Min;
        int segLen = segment.Length;
        double segFit;

        if (minLen > 1)
        {
            segFit = (minLen - segLen) / (double)(minLen - 1);

            if (segFit < 0.0)
                segFit = 0.0;
            else if (segFit > 1.0)
                segFit = 1.0;
        }
        else
        {
            segFit = segLen == 1 ? 1.0 : 0.0;
        }

        //The number of operations is a factor
        ExpressionCounter counter = new ExpressionCounter();
        counter.Visit(expression);
        double countFit = 1.0 / counter.Count;

        return (segFit + countFit) / 2.0;
    }
}