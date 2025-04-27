using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Abstracts;
using Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine.Abstracts;
using Genbox.FastData.Internal.Extensions;
using static Genbox.FastData.Internal.Helpers.DebugHelper;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

internal sealed class GeneticEngine(GeneticEngineConfig config, IGene[] genes)
{
    /// <summary>Runs the evolution process.</summary>
    /// <param name="simulation">
    /// A delegate that should be run as part of the simulation. Callers should run their fitness calculating code and update Entity with the value
    /// (Entity.Fitness)
    /// </param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal Entity Evolve(Simulation simulation, ISelection selection, ICrossOver crossOver, IMutation mutation, IReinsertion reinsertion, ITermination termination, IRandom random)
    {
        //Create the initial population
        StaticArray<Entity> population = new StaticArray<Entity>(config.PopulationSize * 2);

        //We have a secondary list for the new population. We exchange the two arrays each generation
        StaticArray<Entity> newPopulation = new StaticArray<Entity>(config.PopulationSize * 2);

        for (int i = 0; i < config.PopulationSize; i++)
        {
            Entity p = new Entity(genes); // Clone the genes into each entity
            p.ForceMutate(random);
            population.Add(ref p);
        }

        int generation = 0;
        Entity top1 = new Entity([]);
        top1.Fitness = double.MinValue;

        //These lists are for reuse. We clear them each generation.
        List<int> parents = new List<int>();

        do
        {
            Print("Generation: " + generation);

            int bestIdx = RunPopulation(population, simulation);
            ref Entity popBest = ref population[bestIdx];

            if (popBest.Fitness > top1.Fitness)
                top1 = popBest; //This creates a copy, which is what we want, since we clear the population array

            //Note: The algorithm here is derived from my understanding of genetics in biology.
            //      It might be different from established terminology in the comp.sci field

            //Selection is about picking ones that must survive. It starts with the entire population.
            //A selection process can return the same parent multiple times.
            selection.Process(population, parents, config.PopulationSize);

            Debug.Assert(parents.Count <= config.PopulationSize);

            //For simplicity, we assume an equal amount of parents in all our algorithms. We simply remove a parent to make it even.
            if (parents.Count % 2 != 0)
                parents.Remove(parents[parents.Count - 1]);

            if (parents.Count == 0)
                throw new InvalidOperationException("There are no parents selected.");

            if (config.ShuffleParents)
                parents.Shuffle(random);

            //We then cross parents to combine their properties which becomes children. Children are put into the new population
            crossOver.Process(population, parents, newPopulation);

            //We add mutations to the children
            mutation.Process(newPopulation);

            //We re-add some of the parents to the new population.
            reinsertion.Process(population, newPopulation);

            //Two parents produce one or two children. Together with death, it makes the population dwindle. To recuperate, we introduce repopulation
            Repopulate(newPopulation, config.PopulationSize, genes, random);

            //At this point, we switch new population with old population
            population.Clear();
            parents.Clear();
            (population, newPopulation) = (newPopulation, population);

            //Termination of the simulation is given the generation and top fitness.
            //Makes it able to either terminate after a set generation, or when a certain fitness is reached
        } while (!termination.Process(generation++, top1.Fitness));

        return top1;
    }

    private static void Repopulate(StaticArray<Entity> population, int minPopulation, IGene[] genes, IRandom random)
    {
        while (population.Count < minPopulation)
        {
            Entity entity = new Entity(genes);
            entity.ForceMutate(random);
            population.Add(ref entity);
        }
    }

    private static int RunPopulation(StaticArray<Entity> population, Simulation simulation)
    {
        double maxFit = double.MinValue;
        int bestIdx = 0;

        for (int i = 0; i < population.Count; i++)
        {
            ref Entity entity = ref population[i];
            simulation(ref entity);

            // We keep a running max
            if (entity.Fitness > maxFit)
            {
                maxFit = entity.Fitness;
                bestIdx = i;
            }
        }

        return bestIdx;
    }
}