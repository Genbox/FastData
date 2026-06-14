using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Generators.Expressions.Optimizer;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Provides the combine, reduce, and annotate pipeline steps for early exit processing.</summary>
internal static class EarlyExitPipeline
{
    /// <summary>Merges mandatory and analysis-generated early exits into a single deduplicated list.</summary>
    internal static List<IEarlyExit> CombineAndDedup(IEnumerable<IEarlyExit> listA, IEnumerable<IEarlyExit> listB)
    {
        List<IEarlyExit> exits = new List<IEarlyExit>(8);

        foreach (IEarlyExit exit in listA)
            AddExit(exits, exit);

        foreach (IEarlyExit exit in listB)
            AddExit(exits, exit);

        return exits;

        static void AddExit(List<IEarlyExit> exits, IEarlyExit exit)
        {
            for (int i = 0; i < exits.Count; i++)
            {
                //All exits are records, and as such, have value equality
                if (EqualityComparer<IEarlyExit>.Default.Equals(exit, exits[i]))
                    return;
            }

            exits.Add(exit);
        }
    }

    /// <summary>Optimize early exits by reducing the amount and increasing their strength</summary>
    internal static void Optimize<TKey>(List<IEarlyExit> exits)
    {
        Reduce(exits);
        Merge(exits);
        return;

        // Removes early exits that are strictly weaker than another exit in the list.
        static void Reduce(List<IEarlyExit> exits)
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

        // Merges complementary pairs of less-than and greater-than exits into single compound range checks.
        static void Merge(List<IEarlyExit> exits)
        {
            TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

            if (typeCode == TypeCode.String)
            {
                MergeLengthBounds(exits);
                MergeUnitAtBounds(exits);
            }
            else
                MergeNumericBounds<TKey>(exits);
        }
    }

    /// <summary>Converts early exits into annotated expression trees using the specified input parameter.</summary>
    internal static AnnotatedExpr[] Annotate(List<IEarlyExit> exits, ParameterExpression inputKey, ExpressionVisitor? visitor = null)
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

    /// <summary>Applies expression-level optimizations (constant folding, bitwise simplification, comparison reduction) to the annotated expressions.</summary>
    internal static void OptimizeExpressions(AnnotatedExpr[] exprs)
    {
        for (int i = 0; i < exprs.Length; i++)
            exprs[i] = new AnnotatedExpr(ExprOptimizer.Visit(exprs[i].Expression), exprs[i].Kind);
    }

    private static void MergeNumericBounds<TKey>(List<IEarlyExit> exits)
    {
        // Find the LessThan/GreaterThan pair and merge them.
        // It is assumed that reduction has run first, so there should only be the very outer bounds. That is, only one pair.

        ValueLessThanEarlyExit<TKey>? lt = null;
        int ltIdx = -1;

        ValueGreaterThanEarlyExit<TKey>? gt = null;
        int gtIdx = -1;

        for (int i = 0; i < exits.Count; i++)
        {
            IEarlyExit exit = exits[i];

            switch (exit)
            {
                case ValueLessThanEarlyExit<TKey> a:
                    lt = a;
                    ltIdx = i;
                    break;
                case ValueGreaterThanEarlyExit<TKey> b:
                    gt = b;
                    gtIdx = i;
                    break;
            }

            if (lt != null && gt != null)
                break; // We found both. Stop.
        }

        if (lt == null || gt == null)
            return;

        int first = Math.Min(ltIdx, gtIdx);
        int second = Math.Max(ltIdx, gtIdx);
        // Remove the later index first so the earlier index remains valid.
        exits.RemoveAt(second);
        exits.RemoveAt(first);

        ValueRangeEarlyExit<TKey> range = new ValueRangeEarlyExit<TKey>(lt.Value, gt.Value, lt.KeyspaceSize, gt.KeyspaceSize);
        exits.Insert(first, range); //Insert the new compound at the first early exit we saw
    }

    private static void MergeLengthBounds(List<IEarlyExit> exits)
    {
        LengthLessThanEarlyExit? lt = null;
        int ltIdx = -1;

        LengthGreaterThanEarlyExit? gt = null;
        int gtIdx = -1;

        for (int i = 0; i < exits.Count; i++)
        {
            switch (exits[i])
            {
                case LengthLessThanEarlyExit a:
                    lt = a;
                    ltIdx = i;
                    break;
                case LengthGreaterThanEarlyExit b:
                    gt = b;
                    gtIdx = i;
                    break;
            }
        }

        if (lt == null || gt == null)
            return;

        int first = Math.Min(ltIdx, gtIdx);
        int second = Math.Max(ltIdx, gtIdx);
        // Remove the later index first so the earlier index remains valid.
        exits.RemoveAt(second);
        exits.RemoveAt(first);

        LengthRangeEarlyExit range = new LengthRangeEarlyExit(lt.Value, gt.Value);
        exits.Insert(first, range); //Insert the new compound at the first early exit we saw
    }

    private static void MergeUnitAtBounds(List<IEarlyExit> exits)
    {
        // Merge UnitAtLessThan/UnitAtGreaterThan pairs with the same offset.
        // There can be pairs for offset 0 and offset -1.
        for (int i = exits.Count - 1; i >= 0; i--)
        {
            if (exits[i] is not UnitAtLessThanEarlyExit lt)
                continue;

            for (int j = exits.Count - 1; j >= 0; j--)
            {
                if (i == j)
                    continue;

                if (exits[j] is not UnitAtGreaterThanEarlyExit gt || gt.Offset != lt.Offset)
                    continue;

                UnitAtRangeEarlyExit compound = new UnitAtRangeEarlyExit(lt.Value, gt.Value, lt.Offset);

                int first = Math.Min(i, j);
                int second = Math.Max(i, j);
                exits.RemoveAt(second);
                exits.RemoveAt(first);
                exits.Insert(first, compound);

                // Restart since indices changed.
                i = exits.Count;
                break;
            }
        }
    }
}