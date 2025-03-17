using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Analysis;

internal delegate void Simulation<in T, T2>(object[] data, T config, ref Candidate<T2> candidate) where T : AnalyzerConfig where T2 : struct, IHashSpec;