using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Provides the combine, reduce, and annotate pipeline steps for early exit processing.</summary>
public static class EarlyExitPipeline
{
    /// <summary>Merges mandatory and analysis-generated early exits into a single deduplicated list.</summary>
    public static List<IEarlyExit> Combine(IEnumerable<IEarlyExit> mandatory, IEnumerable<IEarlyExit> candidates)
    {
        List<IEarlyExit> exits = new List<IEarlyExit>(8);

        foreach (IEarlyExit exit in mandatory)
            AddExit(exits, exit);

        foreach (IEarlyExit exit in candidates)
            AddExit(exits, exit);

        return exits;
    }

    /// <summary>Removes early exits that are strictly weaker than another exit in the list.</summary>
    public static void Reduce(List<IEarlyExit> exits)
    {
        for (int i = exits.Count - 1; i >= 0; i--)
        {
            IEarlyExit current = exits[i];

            for (int j = exits.Count - 1; j >= 0; j--)
            {
                if (i == j)
                    continue;

                if (current.IsWorseThan(exits[j]))
                {
                    exits.RemoveAt(i);
                    break;
                }
            }
        }
    }

    /// <summary>Converts early exits into annotated expression trees using the specified input parameter.</summary>
    public static AnnotatedExpr[] Annotate(List<IEarlyExit> exits, ParameterExpression inputKey, ExpressionVisitor? visitor = null)
    {
        AnnotatedExpr[] exprs = new AnnotatedExpr[exits.Count];

        for (int i = 0; i < exits.Count; i++)
        {
            Expression expression = exits[i].GetExpression(inputKey);
            visitor?.Visit(expression);
            exprs[i] = new AnnotatedExpr(expression, ExprKind.EarlyExit);
        }

        return exprs;
    }

    private static void AddExit(List<IEarlyExit> exits, IEarlyExit exit)
    {
        for (int i = 0; i < exits.Count; i++)
        {
            if (EqualityComparer<IEarlyExit>.Default.Equals(exit, exits[i]))
                return;
        }

        exits.Add(exit);
    }
}