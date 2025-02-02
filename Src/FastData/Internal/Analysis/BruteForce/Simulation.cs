using Genbox.FastData.Internal.Analysis.Genetic;

namespace Genbox.FastData.Internal.Analysis.BruteForce;

internal delegate void Simulation<in T, T2>(object[] data, T settings, ref Candidate<T2> candidate) where T : CommonSettings where T2 : struct, IHashSpec;