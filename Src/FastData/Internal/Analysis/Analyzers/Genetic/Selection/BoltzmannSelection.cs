using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Helpers;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Selection;

/// <summary>
/// Boltzmann Selection is a method that dynamically adjusts selection pressure using a temperature parameter. Unlike traditional selection methods that fix selection probabilities based on fitness, Boltzmann Selection gradually shifts from exploration (randomness) to exploitation (favoring
/// high-fitness individuals) over time. It is inspired by simulated annealing, where higher temperatures allow more randomness, and lower temperatures favor the best solutions.
/// </summary>
internal sealed class BoltzmannSelection : ISelection
{
    private readonly double _temperature;
    private readonly IRandom _random;
    private double[] _cumulative = [];

    /// <summary>Selects parents with probabilities that follow the Boltzmann distribution of their fitness.</summary>
    /// <param name="temperature">The selection temperature. Must be a finite number greater than 0. Higher values give more randomness.</param>
    /// <param name="random">The random source used to choose parents.</param>
    public BoltzmannSelection(double temperature, IRandom random)
    {
        if (temperature is <= 0 or double.NaN or double.PositiveInfinity)
            throw new ArgumentOutOfRangeException(nameof(temperature), temperature, "Must be a finite number greater than 0.");

        _temperature = temperature;
        _random = random;
    }

    public void Process(StaticArray<Entity> population, List<int> parents, int maxParents)
    {
        int count = population.Count;
        if (_cumulative.Length < count)
            _cumulative = new double[count];

        double maxFitness = population[0].Fitness;
        for (int i = 1; i < count; i++)
            maxFitness = Math.Max(maxFitness, population[i].Fitness);

        double sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += Math.Exp((population[i].Fitness - maxFitness) / _temperature);
            _cumulative[i] = sum;
        }

        for (int i = 0; i < maxParents; i++)
            parents.Add(GeneHelper.FindSlice(_cumulative, count, _random.NextDouble() * sum));
    }
}