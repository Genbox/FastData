using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.BruteForce;

internal delegate void Simulation<in T, T2>(object[] data, T config, ref Candidate<T2> candidate) where T : AnalyzerConfig where T2 : struct, IHashSpec;