using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Models;

public readonly record struct PerfectHashGPerfContext(int[] AssociationValues, int[] AlphaIncrements, KeyValuePair<string, uint>[] Items, int[] Positions, int MaxHash) : IContext;