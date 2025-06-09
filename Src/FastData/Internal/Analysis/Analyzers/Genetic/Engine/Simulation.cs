namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

internal delegate void Simulation<T>(ReadOnlySpan<T> data, ref Entity entity);